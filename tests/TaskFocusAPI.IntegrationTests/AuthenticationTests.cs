using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TaskFocusAPI.Data;
using TaskFocusAPI.Library.Models;
using TaskFocusAPI.Models;

namespace TaskFocusAPI.IntegrationTests
{
    [Collection(ApiCollection.Name)]
    public sealed class AuthenticationTests(ApiFixture fixture)
    {
        private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

        [Fact]
        public async Task TokenReturnsJwtForValidCredentials()
        {
            var user = await fixture.CreateUserAsync();
            const string additionalRole = "TokenTestRole";
            await using (var scope = fixture.Factory.Services.CreateAsyncScope())
            {
                var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                Assert.True((await roles.CreateAsync(new IdentityRole(additionalRole))).Succeeded);
                var trackedUser = await users.FindByIdAsync(user.Id);
                Assert.NotNull(trackedUser);
                Assert.True((await users.AddToRoleAsync(trackedUser, additionalRole)).Succeeded);
            }

            var emailLookups = new EmailLookupCounter();
            await using var factory = fixture.Factory.WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services => services.ConfigureDbContext<ApplicationDbContext>(
                    options => options.AddInterceptors(emailLookups))));
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
            string username = user.Email!.ToUpperInvariant();
            using var response = await ApiFixture.RequestTokenAsync(client, username, ApiFixture.Password);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
            Assert.Equal(new[] { "accessToken", "userName" }, json.EnumerateObject().Select(field => field.Name).Order());
            Assert.Equal(username, json.GetProperty("userName").GetString());
            var accessToken = json.GetProperty("accessToken").GetString();
            Assert.NotNull(accessToken);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            Assert.Equal(username, Assert.Single(jwt.Claims, claim => claim.Type == ClaimTypes.Name).Value);
            Assert.Equal(user.Id, Assert.Single(jwt.Claims, claim => claim.Type == ClaimTypes.NameIdentifier).Value);
            Assert.Equal(new[] { additionalRole, "User" }, jwt.Claims.Where(claim => claim.Type == ClaimTypes.Role)
                .Select(claim => claim.Value).Order());
            Assert.Equal(1, emailLookups.Count);
        }

        [Fact]
        public async Task TokenRejectsWrongPassword()
        {
            var user = await fixture.CreateUserAsync();
            using var client = fixture.CreateClient();
            using var response = await ApiFixture.RequestTokenAsync(client, user.Email!, "incorrect-password");

            await AssertInvalidLoginAsync(response);
        }

        [Fact]
        public async Task TokenRejectsUnknownUser()
        {
            using var client = fixture.CreateClient();
            using var response = await ApiFixture.RequestTokenAsync(client, ApiFixture.NewEmail(), ApiFixture.Password);

            await AssertInvalidLoginAsync(response);
        }

        [Fact]
        public async Task TokenRejectsUnconfirmedUser()
        {
            var user = await fixture.CreateUserAsync(confirmed: false);
            using var client = fixture.CreateClient();
            using var response = await ApiFixture.RequestTokenAsync(client, user.Email!, ApiFixture.Password);

            await AssertInvalidLoginAsync(response);
        }

        [Fact]
        public async Task TokenRejectsTwoFactorEnabledUser()
        {
            var user = await fixture.CreateUserAsync(twoFactorEnabled: true);
            using var client = fixture.CreateClient();
            using var response = await ApiFixture.RequestTokenAsync(client, user.Email!, ApiFixture.Password);

            await AssertInvalidLoginAsync(response);
            Assert.False(await CheckPasswordAsync(client, user.Email!, ApiFixture.Password));
        }

        [Fact]
        public async Task TokenLocksAccountAfterFiveWrongPasswords()
        {
            var user = await fixture.CreateUserAsync();
            using var client = fixture.CreateClient();

            await FailTokenAttemptsAsync(4);
            using var beforeLockout = await ApiFixture.RequestTokenAsync(client, user.Email!, ApiFixture.Password);
            Assert.Equal(HttpStatusCode.OK, beforeLockout.StatusCode);
            Assert.Equal(0, (await ReadIdentityUserAsync(user.Id)).AccessFailedCount);

            await FailTokenAttemptsAsync(5);
            using var afterLockout = await ApiFixture.RequestTokenAsync(client, user.Email!, ApiFixture.Password);
            await AssertInvalidLoginAsync(afterLockout);

            async Task FailTokenAttemptsAsync(int attempts)
            {
                for (var i = 0; i < attempts; i++)
                {
                    using var response = await ApiFixture.RequestTokenAsync(client, user.Email!, "incorrect-password");
                    await AssertInvalidLoginAsync(response);
                }
            }
        }

        [Fact]
        public async Task TokenLocksUnconfirmedAccountAfterFiveWrongPasswords()
        {
            var user = await fixture.CreateUserAsync(confirmed: false);
            using var client = fixture.CreateClient();

            for (var i = 0; i < 5; i++)
            {
                using var response = await ApiFixture.RequestTokenAsync(client, user.Email!, "incorrect-password");
                await AssertInvalidLoginAsync(response);
            }

            Assert.True((await ReadIdentityUserAsync(user.Id)).LockoutEnd > DateTimeOffset.UtcNow);
            Assert.False(await CheckPasswordAsync(client, user.Email!, ApiFixture.Password));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task PasswordCheckLocksAccountAfterFiveWrongPasswords(bool confirmed)
        {
            var user = await fixture.CreateUserAsync(confirmed);
            using var client = fixture.CreateClient();

            Assert.False(await CheckPasswordAsync(client, ApiFixture.NewEmail(), ApiFixture.Password));
            for (var i = 0; i < 4; i++)
            {
                Assert.False(await CheckPasswordAsync(client, user.Email!, "incorrect-password"));
            }

            // The web can distinguish a valid password from a failed attempt before showing the confirmation page.
            Assert.True(await CheckPasswordAsync(client, user.Email!, ApiFixture.Password));
            Assert.Equal(0, (await ReadIdentityUserAsync(user.Id)).AccessFailedCount);
            for (var i = 0; i < 5; i++)
            {
                Assert.False(await CheckPasswordAsync(client, user.Email!, "incorrect-password"));
            }

            Assert.False(await CheckPasswordAsync(client, user.Email!, ApiFixture.Password));
        }

        [Fact]
        public async Task LockedAccountCanResetPasswordAndSignIn()
        {
            var user = await fixture.CreateUserAsync();
            await LockAccountAsync(user.Id);
            using var client = fixture.CreateClient();
            using var emailResponse = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/SendPasswordResetEmail",
                new UserModel { Email = user.Email! }, CancellationToken);
            Assert.Equal(HttpStatusCode.OK, emailResponse.StatusCode);
            Assert.True(fixture.Emails.PasswordResetLinks.TryGetValue(user.Email!, out string? link));
            var query = QueryHelpers.ParseQuery(new Uri(link).Query);

            const string newPassword = "New-fixture-password!234";
            using var reset = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/ResetPassword",
                new ResetPasswordModel
                {
                    Email = query["email"].ToString(),
                    Token = query["token"].ToString(),
                    Password = newPassword,
                    ConfirmPassword = newPassword
                }, CancellationToken);
            Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
            await AssertPasswordChangeAllowsSignInAsync(client, user, newPassword);
        }

        [Fact]
        public async Task LockedAccountCanChangePasswordFromExistingSessionAndSignIn()
        {
            var user = await fixture.CreateUserAsync();
            using var session = await fixture.CreateAuthenticatedClientAsync(user);
            await LockAccountAsync(user.Id);

            const string newPassword = "New-fixture-password!234";
            using var update = await HttpClientJsonExtensions.PutAsJsonAsync(session, "/api/User/UpdatePassword",
                new UserRegistrationModel("Test", "User", user.Email!, newPassword), CancellationToken);
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);

            using var client = fixture.CreateClient();
            await AssertPasswordChangeAllowsSignInAsync(client, user, newPassword);
        }

        [Fact]
        public async Task StockIdentityRegistrationPagesAreNotServed()
        {
            var user = await fixture.CreateUserAsync(confirmed: false);
            using var client = fixture.CreateClient();
            using var register = await client.GetAsync("/Identity/Account/Register", CancellationToken);
            using var confirmation = await client.GetAsync(QueryHelpers.AddQueryString(
                "/Identity/Account/RegisterConfirmation", "email", user.Email!), CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, register.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, confirmation.StatusCode);
        }

        [Fact]
        public async Task TaskListRequiresAuthentication()
        {
            using var client = fixture.CreateClient();
            using var response = await client.GetAsync("/api/Task/GetAllTasksForUser", CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RegistrationSendsAUsableEmailConfirmationLink()
        {
            using var client = fixture.CreateClient();
            string email = ApiFixture.NewEmail();
            using var registration = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/Register",
                new UserRegistrationModel("Test", "User", email, ApiFixture.Password), CancellationToken);
            Assert.Equal(HttpStatusCode.OK, registration.StatusCode);

            Assert.True(fixture.Emails.ConfirmationLinks.TryGetValue(email, out string? link));
            var confirmationLink = new Uri(link);
            Assert.Equal(TaskFocusApiFactory.AppUrl, confirmationLink.GetLeftPart(UriPartial.Authority));
            Assert.Equal("/unconfirmedemail", confirmationLink.AbsolutePath);
            var query = QueryHelpers.ParseQuery(confirmationLink.Query);
            Assert.Equal(email, query["email"].ToString());
            string token = query["token"].ToString();
            Assert.False(string.IsNullOrWhiteSpace(token));

            using var confirmation = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/ConfirmEmail",
                new ConfirmEmailModel { Email = email, Token = token }, CancellationToken);
            Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
        }

        [Fact]
        public async Task NewUserCanReadDefaultSettings()
        {
            var user = await fixture.CreateUserAsync();
            using var client = await fixture.CreateAuthenticatedClientAsync(user);
            var settings = await client.GetFromJsonAsync<JsonElement>("/api/User/GetCurrentUserSettings", CancellationToken);

            Assert.Equal(user.Id, settings.GetProperty("id").GetString()?.TrimEnd(' '));
            Assert.False(settings.GetProperty("cleanUpImmediately").GetBoolean());
            Assert.Equal(7, settings.GetProperty("cleanUpDelayDays").GetInt32());
            Assert.Equal(30, settings.GetProperty("deleteDelayDays").GetInt32());
        }

        private async Task LockAccountAsync(string userId)
        {
            await using var scope = fixture.Factory.Services.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var account = await users.FindByIdAsync(userId);
            Assert.NotNull(account);
            account.AccessFailedCount = 4;
            Assert.True((await users.SetLockoutEndDateAsync(account, DateTimeOffset.UtcNow.AddMinutes(5))).Succeeded);
        }

        private async Task<IdentityUser> ReadIdentityUserAsync(string userId)
        {
            await using var scope = fixture.Factory.Services.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await users.FindByIdAsync(userId);
            Assert.NotNull(user);
            return user;
        }

        private async Task AssertPasswordChangeAllowsSignInAsync(HttpClient client, IdentityUser user, string newPassword)
        {
            var saved = await ReadIdentityUserAsync(user.Id);
            Assert.Null(saved.LockoutEnd);
            Assert.Equal(0, saved.AccessFailedCount);
            Assert.True(await CheckPasswordAsync(client, user.Email!, newPassword));
            using var signIn = await ApiFixture.RequestTokenAsync(client, user.Email!, newPassword);
            Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
        }

        private static async Task AssertInvalidLoginAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsStringAsync(CancellationToken));
        }

        private static async Task<bool> CheckPasswordAsync(HttpClient client, string email, string password)
        {
            using var response = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/CheckPasswordValid",
                new CheckPasswordModel { Email = email, Password = password }, CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<bool>(CancellationToken);
        }

        private sealed class EmailLookupCounter : DbCommandInterceptor
        {
            public int Count { get; private set; }

            public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
                CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken)
            {
                int where = command.CommandText.IndexOf("WHERE", StringComparison.Ordinal);
                if (where >= 0 && command.CommandText.Contains("FROM [AspNetUsers]", StringComparison.Ordinal) &&
                    command.CommandText.AsSpan(where).Contains("[NormalizedEmail]", StringComparison.Ordinal))
                {
                    Count++;
                }

                return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
            }
        }
    }
}
