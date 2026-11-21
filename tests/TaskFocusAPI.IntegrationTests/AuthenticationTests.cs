using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
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
            using var client = fixture.CreateClient();
            using var response = await ApiFixture.RequestTokenAsync(client, user.Email!, ApiFixture.Password);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var token = await response.Content.ReadFromJsonAsync<ApiFixture.TokenResponse>(CancellationToken);
            Assert.NotNull(token);
            Assert.False(string.IsNullOrWhiteSpace(token.AccessToken));
            Assert.Equal(user.Email, token.UserName);
        }

        [Fact]
        public async Task TokenRejectsWrongPassword()
        {
            var user = await fixture.CreateUserAsync();
            using var client = fixture.CreateClient();
            using var response = await ApiFixture.RequestTokenAsync(client, user.Email!, "incorrect-password");

            AssertClientError(response);
        }

        [Fact]
        public async Task TokenRejectsUnknownUser()
        {
            using var client = fixture.CreateClient();
            using var response = await ApiFixture.RequestTokenAsync(client, ApiFixture.NewEmail(), ApiFixture.Password);

            AssertClientError(response);
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

        private static void AssertClientError(HttpResponseMessage response)
        {
            Assert.InRange((int)response.StatusCode, 400, 499);
        }
    }
}
