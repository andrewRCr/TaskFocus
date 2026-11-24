using System.Net;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Tests;

public sealed class ApiRequestTests
{
    [Fact]
    public async Task SuccessfulRequestReturnsResponseForCallerToRead()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("true") };

        var result = await ApiRequest.SendAsync(Task.FromResult(response));

        Assert.Same(response, result);
        Assert.Equal("true", await result.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(400, "Could not complete the request. Please try again.", false)]
    [InlineData(401, "Could not complete the request. Please try again.", false)]
    [InlineData(408, ApiRequest.ServiceUnavailableMessage, true)]
    [InlineData(429, "There have been too many attempts. Please wait and try again.", false)]
    [InlineData(500, ApiRequest.ServiceUnavailableMessage, true)]
    [InlineData(503, ApiRequest.ServiceUnavailableMessage, true)]
    [InlineData(599, ApiRequest.ServiceUnavailableMessage, true)]
    public async Task FailedResponseUsesPlainTextAndPreservesStatus(int status, string message, bool isServiceFailure)
    {
        using var response = FailureResponse((HttpStatusCode)status);

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => ApiRequest.SendAsync(Task.FromResult(response)));

        Assert.Equal((HttpStatusCode)status, error.StatusCode);
        Assert.Equal(message, error.Message);
        Assert.Equal(isServiceFailure, ApiRequest.IsServiceFailure(error));
        Assert.Throws<ObjectDisposedException>(() => response.Content.ReadAsStream(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConnectionFailureAndTimeoutUseServiceMessage(bool timeout)
    {
        Exception cause = timeout ? new TaskCanceledException("Technical timeout") : new HttpRequestException("Technical network error");

        var error = await Assert.ThrowsAsync<HttpRequestException>(
            () => ApiRequest.SendAsync(Task.FromException<HttpResponseMessage>(cause)));

        Assert.Null(error.StatusCode);
        Assert.Equal(ApiRequest.ServiceUnavailableMessage, error.Message);
        Assert.True(ApiRequest.IsServiceFailure(error));
    }

    [Theory]
    [InlineData("context", "/api/context/GetAllContextsForUser")]
    [InlineData("project", "/api/project/GetAllProjectsForUser")]
    [InlineData("task", "/api/task/GetAllTasksForUser")]
    [InlineData("user", "/api/user/GetCurrentUserSettings")]
    public async Task DataEndpointsUsePlainServiceFailure(string endpoint, string expectedPath)
    {
        using var handler = new FailingHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.invalid") };
        var apiHelper = new ApiHelper(client);

        Task request = endpoint switch
        {
            "context" => new ContextEndpoint(apiHelper).GetAllContextsForUser(),
            "project" => new ProjectEndpoint(apiHelper).GetAllProjectsForUser(),
            "task" => new TaskEndpoint(apiHelper).GetAllTasksForUser(),
            "user" => new UserEndpoint(apiHelper).GetCurrentUserSettings(),
            _ => throw new ArgumentOutOfRangeException(nameof(endpoint))
        };

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, error.StatusCode);
        Assert.Equal(ApiRequest.ServiceUnavailableMessage, error.Message);
        Assert.Equal(expectedPath, handler.Path);
    }

    private static HttpResponseMessage FailureResponse(HttpStatusCode status) => new(status)
    {
        ReasonPhrase = "Proxy diagnostics",
        Content = new StringContent("""{"message":"Internal diagnostics"}""")
    };

    private sealed class FailingHandler : HttpMessageHandler
    {
        public string? Path { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri?.PathAndQuery;
            return Task.FromResult(FailureResponse(HttpStatusCode.ServiceUnavailable));
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
