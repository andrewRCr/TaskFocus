using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TaskFocusAPI.Library.Models;
using TaskFocusAPI.Models;

namespace TaskFocusAPI.IntegrationTests
{
    [Collection(ApiCollection.Name)]
    public sealed class AccountOperationTests(ApiFixture fixture)
    {
        private const string NewPassword = "New-fixture-password!123";
        private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

        [Fact]
        public async Task ReusedPasswordResetLinkReturnsBadRequestAndSendsNoSecondNotice()
        {
            var user = await fixture.CreateUserAsync();
            using var client = fixture.CreateClient();
            var model = await RequestResetAsync(client, user.Email!, NewPassword);
            int previousMessageCount = fixture.Emails.Messages.Count;

            using var firstReset = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/ResetPassword", model, CancellationToken);
            Assert.Equal(HttpStatusCode.OK, firstReset.StatusCode);
            var success = await firstReset.Content.ReadFromJsonAsync<Response>(CancellationToken);
            Assert.Equal("Success", success?.Status);
            Assert.Equal("Password changed successfully.", success?.Message);
            string? passwordHash = (await fixture.FindIdentityUserAsync(user.Id))?.PasswordHash;
            Assert.NotEqual(user.PasswordHash, passwordHash);
            var notice = Assert.Single(fixture.Emails.Messages.Skip(previousMessageCount));
            Assert.Equal("Password changed", notice.Subject);

            using var secondReset = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/ResetPassword", model, CancellationToken);
            await AssertPasswordRejectionAsync(secondReset, "InvalidToken");
            Assert.Equal(passwordHash, (await fixture.FindIdentityUserAsync(user.Id))?.PasswordHash);
            Assert.Single(fixture.Emails.Messages.Skip(previousMessageCount));
        }

        [Fact]
        public async Task PasswordResetRejectedByPolicyReturnsBadRequestAndSendsNoNotice()
        {
            var user = await fixture.CreateUserAsync();
            using var client = fixture.CreateClient();
            var model = await RequestResetAsync(client, user.Email!, "weak");
            int previousMessageCount = fixture.Emails.Messages.Count;

            using var reset = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/ResetPassword", model, CancellationToken);

            await AssertPasswordRejectionAsync(reset, "PasswordTooShort");
            Assert.Equal(user.PasswordHash, (await fixture.FindIdentityUserAsync(user.Id))?.PasswordHash);
            Assert.Equal(previousMessageCount, fixture.Emails.Messages.Count);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task RejectedPasswordUpdateUsesRequestedStatusAndSendsNoNotice(bool reportRejection)
        {
            var user = await fixture.CreateUserAsync();
            using var client = await fixture.CreateAuthenticatedClientAsync(user);
            int previousMessageCount = fixture.Emails.Messages.Count;
            string path = reportRejection ? "/api/User/UpdatePassword?reportRejection=true" : "/api/User/UpdatePassword";

            using var update = await HttpClientJsonExtensions.PutAsJsonAsync(client, path,
                new UserRegistrationModel("Test", "User", user.Email!, "weak"), CancellationToken);

            if (reportRejection)
            {
                await AssertPasswordRejectionAsync(update, "PasswordTooShort");
            }
            else
            {
                Assert.Equal(HttpStatusCode.OK, update.StatusCode);
                Assert.Empty(await update.Content.ReadAsStringAsync(CancellationToken));
            }
            Assert.Equal(user.PasswordHash, (await fixture.FindIdentityUserAsync(user.Id))?.PasswordHash);
            Assert.Equal(previousMessageCount, fixture.Emails.Messages.Count);
        }

        [Theory]
        [InlineData("/api/User/UpdatePassword")]
        [InlineData("/api/User/UpdatePassword?reportRejection=true")]
        public async Task PasswordUpdateRetrySendsOnlyOneNoticeAndLaterChangeSendsAnother(string path)
        {
            const string laterPassword = "Later-fixture-password!456";
            var user = await fixture.CreateUserAsync();
            using var client = await fixture.CreateAuthenticatedClientAsync(user);
            var update = new UserRegistrationModel("Test", "User", user.Email!, NewPassword);
            int previousMessageCount = fixture.Emails.Messages.Count;

            using var first = await HttpClientJsonExtensions.PutAsJsonAsync(client, path, update, CancellationToken);
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Empty(await first.Content.ReadAsStringAsync(CancellationToken));
            var firstSaved = Assert.IsType<IdentityUser>(await fixture.FindIdentityUserAsync(user.Id));
            Assert.NotEqual(user.PasswordHash, firstSaved.PasswordHash);
            var firstNotice = Assert.Single(fixture.Emails.Messages.Skip(previousMessageCount));
            Assert.Equal("Password changed", firstNotice.Subject);
            using var firstSignIn = await ApiFixture.RequestTokenAsync(client, user.Email!, NewPassword);
            Assert.Equal(HttpStatusCode.OK, firstSignIn.StatusCode);

            using var retry = await HttpClientJsonExtensions.PutAsJsonAsync(client, path, update, CancellationToken);
            Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
            Assert.Empty(await retry.Content.ReadAsStringAsync(CancellationToken));
            Assert.Single(fixture.Emails.Messages.Skip(previousMessageCount));
            var retrySaved = Assert.IsType<IdentityUser>(await fixture.FindIdentityUserAsync(user.Id));
            Assert.Equal(firstSaved.PasswordHash, retrySaved.PasswordHash);
            Assert.Equal(firstSaved.SecurityStamp, retrySaved.SecurityStamp);

            using var later = await HttpClientJsonExtensions.PutAsJsonAsync(client, path,
                new UserRegistrationModel("Test", "User", user.Email!, laterPassword), CancellationToken);
            Assert.Equal(HttpStatusCode.OK, later.StatusCode);
            Assert.Empty(await later.Content.ReadAsStringAsync(CancellationToken));
            var laterSaved = Assert.IsType<IdentityUser>(await fixture.FindIdentityUserAsync(user.Id));
            Assert.NotEqual(firstSaved.PasswordHash, laterSaved.PasswordHash);
            var laterNotice = Assert.Single(fixture.Emails.Messages.Skip(previousMessageCount + 1));
            Assert.Equal("Password changed", laterNotice.Subject);
            using var laterSignIn = await ApiFixture.RequestTokenAsync(client, user.Email!, laterPassword);
            Assert.Equal(HttpStatusCode.OK, laterSignIn.StatusCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t")]
        public async Task EmailChangeRequestRejectsMissingOrBlankAddressWithoutSendingMail(string? email)
        {
            var user = await fixture.CreateUserAsync();
            using var client = await fixture.CreateAuthenticatedClientAsync(user);
            int previousMessageCount = fixture.Emails.Messages.Count;

            using var request = await HttpClientJsonExtensions.PutAsJsonAsync(client, "/api/User/RequestUpdateEmail",
                new ApplicationUserModel { Id = user.Id, Email = email }, CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, request.StatusCode);
            Assert.Equal(previousMessageCount, fixture.Emails.Messages.Count);
        }

        [Theory]
        [InlineData("ConfirmEmail", false)]
        [InlineData("ConfirmEmail", true)]
        [InlineData("ConfirmUpdatedEmail", false)]
        [InlineData("ConfirmUpdatedEmail", true)]
        public async Task EmailConfirmationRejectsInvalidTokensAndUnknownAccounts(string action, bool accountExists)
        {
            var user = accountExists ? await fixture.CreateUserAsync(confirmed: false) : null;
            string email = user?.Email ?? ApiFixture.NewEmail();
            using var client = fixture.CreateClient();

            using var confirmation = await HttpClientJsonExtensions.PostAsJsonAsync(client, $"/api/User/{action}",
                new { Email = email, OldEmail = email, NewEmail = ApiFixture.NewEmail(), Token = "invalid-token" }, CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, confirmation.StatusCode);
            var error = await confirmation.Content.ReadFromJsonAsync<Response>(CancellationToken);
            Assert.Equal("Error", error?.Status);
            Assert.Contains(accountExists ? "tokens do not match" : "user does not exist", error?.Message);
            if (user != null)
            {
                var persisted = Assert.IsType<IdentityUser>(await fixture.FindIdentityUserAsync(user.Id));
                Assert.Equal(email, persisted.Email);
                Assert.False(persisted.EmailConfirmed);
            }
        }

        [Fact]
        public async Task UpdatedEmailConfirmationReportsRejectedSignInName()
        {
            var user = await fixture.CreateUserAsync();
            string newEmail = ApiFixture.NewEmail();
            await using (var scope = fixture.Factory.Services.CreateAsyncScope())
            {
                var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                // Reserve the sign-in name without reserving the email address.
                var otherUser = new IdentityUser(newEmail) { Email = ApiFixture.NewEmail() };
                Assert.True((await users.CreateAsync(otherUser)).Succeeded);
            }
            using var client = await fixture.CreateAuthenticatedClientAsync(user);
            using var request = await HttpClientJsonExtensions.PutAsJsonAsync(client, "/api/User/RequestUpdateEmail",
                new ApplicationUserModel { Id = user.Id, Email = newEmail }, CancellationToken);
            Assert.Equal(HttpStatusCode.OK, request.StatusCode);

            using var confirmation = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/ConfirmUpdatedEmail",
                new ConfirmUpdatedEmailModel { OldEmail = user.Email!, NewEmail = newEmail, Token = fixture.Tokens.GeneratedTokens[user.Email!] }, CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, confirmation.StatusCode);
            var error = await confirmation.Content.ReadFromJsonAsync<Response>(CancellationToken);
            Assert.Equal("Error", error?.Status);
            Assert.Equal("Could not update the sign-in name.", error?.Message);
        }

        private async Task<ResetPasswordModel> RequestResetAsync(HttpClient client, string email, string password)
        {
            using var request = await HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/User/SendPasswordResetEmail",
                new UserModel { Email = email }, CancellationToken);
            Assert.Equal(HttpStatusCode.OK, request.StatusCode);
            return new ResetPasswordModel
            {
                Email = email,
                Token = fixture.Tokens.GeneratedTokens[email],
                Password = password,
                ConfirmPassword = password
            };
        }

        private static async Task AssertPasswordRejectionAsync(HttpResponseMessage response, string errorCode)
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var errors = await response.Content.ReadFromJsonAsync<Dictionary<string, string[]>>(CancellationToken);
            Assert.NotNull(errors);
            Assert.NotEmpty(Assert.Single(errors[errorCode]));
        }
    }
}
