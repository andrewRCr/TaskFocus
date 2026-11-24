using System.Net;
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
                await Assert.ThrowsAsync<Exception>(() => service.RequestUpdateEmail(emailUpdate));
            }
            else
            {
                await Assert.ThrowsAsync<Exception>(() => service.UpdatePassword(passwordUpdate));
            }

            await service.UpdatePassword(passwordUpdate);
            Assert.True(await service.RequestUpdateEmail(emailUpdate));

            Assert.Equal(new[] { failEmailRequest ? emailPath : passwordPath, passwordPath, emailPath }, handler.Paths);
        }

        private sealed class AccountRequestHandler : HttpMessageHandler
        {
            public List<string> Paths { get; } = [];

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Paths.Add(request.RequestUri!.PathAndQuery);
                return Task.FromResult(new HttpResponseMessage(Paths.Count == 1 ? HttpStatusCode.BadRequest : HttpStatusCode.OK));
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
