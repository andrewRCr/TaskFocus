using System.Net;
using System.Text;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Data.Services;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Tests
{
    public sealed class AccountUpdateTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task FailedAccountRequestReleasesGuardForLaterEdits(bool failEmailRequest)
        {
            const string emailPath = "/api/User/RequestUpdateEmail";
            const string passwordPath = "/api/User/UpdatePassword?reportRejection=true";
            using var handler = new AccountRequestHandler();
            using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.invalid") };
            var endpoint = new UserEndpoint(new ApiHelper(client));
            var service = new DataService(null!, null!, null!, endpoint, null!, null!, null!, null!);
            var emailUpdate = new UserModel { Email = "new@example.invalid" };
            var passwordUpdate = new CreateUserModel { Password = "New-test-password!123" };

            if (failEmailRequest)
            {
                await Assert.ThrowsAsync<HttpRequestException>(() => service.RequestUpdateEmail(emailUpdate));
            }
            else
            {
                await Assert.ThrowsAsync<HttpRequestException>(() => service.UpdatePassword(passwordUpdate));
            }

            await service.UpdatePassword(passwordUpdate);
            Assert.True(await service.RequestUpdateEmail(emailUpdate));

            Assert.Equal(new[] { failEmailRequest ? emailPath : passwordPath, passwordPath, emailPath }, handler.Paths);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, """{"status":"Error","message":"Request a new confirmation link."}""",
            "Request a new confirmation link.")]
        [InlineData(HttpStatusCode.BadRequest, """{"Message":"Request a new confirmation link.","Other":["Ignored."]}""",
            "Request a new confirmation link.")]
        [InlineData(HttpStatusCode.BadRequest, """{"PasswordTooShort":["","Use a longer password.","Include a digit."],"Other":["Ignored."]}""",
            "Use a longer password. Include a digit.")]
        [InlineData(HttpStatusCode.BadRequest, """{"title":"Validation failed","errors":{"Password":["Enter a password."]}}""",
            "Enter a password.")]
        [InlineData(HttpStatusCode.BadRequest, "", "The account request was rejected. Check your details and try again.")]
        [InlineData(HttpStatusCode.BadRequest, "<html>Internal diagnostics</html>",
            "The account request was rejected. Check your details and try again.")]
        [InlineData(HttpStatusCode.Unauthorized, "", "This account request is not allowed. Please sign in to continue.")]
        [InlineData(HttpStatusCode.Forbidden, "", "This account request is not allowed. Please sign in to continue.")]
        [InlineData(HttpStatusCode.Gone, "", "This account link is invalid or has expired. Please request a new link.")]
        [InlineData(HttpStatusCode.TooManyRequests, "", "There have been too many attempts. Please wait and try again.")]
        [InlineData(HttpStatusCode.ServiceUnavailable, "", "The account service is unavailable. Please try again later.")]
        public async Task RejectedPasswordResetThrowsWithReadableMessage(HttpStatusCode status, string body, string expected)
        {
            using var handler = new AccountRequestHandler(() => new HttpResponseMessage(status)
            {
                ReasonPhrase = "",
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
            using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.invalid") };
            var endpoint = new UserEndpoint(new ApiHelper(client));

            var error = await Assert.ThrowsAsync<HttpRequestException>(() => endpoint.ResetPassword(new ResetPasswordModel()));

            Assert.Equal(expected, error.Message);
            Assert.Equal(new[] { "/api/User/ResetPassword" }, handler.Paths);
        }

        [Fact]
        public async Task RejectionWithUndecodableTextUsesTheFallbackMessage()
        {
            // 0xFF is never valid UTF-8, so reading the message string throws.
            byte[] body = [.. Encoding.ASCII.GetBytes("{\"message\":\""), 0xFF, .. Encoding.ASCII.GetBytes("\"}")];
            using var handler = new AccountRequestHandler(() => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new ByteArrayContent(body)
            });
            using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.invalid") };
            var endpoint = new UserEndpoint(new ApiHelper(client));

            var error = await Assert.ThrowsAsync<HttpRequestException>(() => endpoint.ResetPassword(new ResetPasswordModel()));

            Assert.Equal("The account request was rejected. Check your details and try again.", error.Message);
        }

        [Fact]
        public async Task AccountTransportFailureThrowsWithConnectionMessage()
        {
            using var handler = new AccountRequestHandler(() => throw new HttpRequestException("Connection refused: internal details."));
            using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.invalid") };
            var endpoint = new UserEndpoint(new ApiHelper(client));

            var error = await Assert.ThrowsAsync<HttpRequestException>(() => endpoint.ConfirmEmail(new ConfirmEmailModel()));

            Assert.Equal("Could not connect to the account service. Check your connection and try again.", error.Message);
            Assert.Equal(new[] { "/api/User/ConfirmEmail" }, handler.Paths);
        }

        private sealed class AccountRequestHandler(Func<HttpResponseMessage>? respond = null) : HttpMessageHandler
        {
            public List<string> Paths { get; } = [];

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Paths.Add(request.RequestUri!.PathAndQuery);
                return Task.FromResult(respond?.Invoke() ??
                    new HttpResponseMessage(Paths.Count == 1 ? HttpStatusCode.BadRequest : HttpStatusCode.OK));
            }
        }

        private sealed class ApiHelper(HttpClient client) : IAPIHelper
        {
            public HttpClient APIClient => client;
            public Task<AuthenticatedUser> AuthenticateAsync(string username, string password) => throw new NotSupportedException();
            public string GetLoggedInUserId() => throw new NotSupportedException();
            public Task GetLoggedInUserInfoAsync(string token) => throw new NotSupportedException();
            public void LogOutUser() => throw new NotSupportedException();
        }
    }
}
