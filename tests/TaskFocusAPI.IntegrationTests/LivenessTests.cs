using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace TaskFocusAPI.IntegrationTests
{
    public sealed class LivenessTests
    {
        [Theory]
        [InlineData("http://localhost")]
        [InlineData("https://localhost")]
        public async Task HealthReturnsOkWithoutAuthenticationOrDatabase(string baseAddress)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            await using var factory = new TaskFocusApiFactory(string.Empty, string.Empty);
            await using var livenessFactory = factory.WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                {
                    services.Configure<HttpsRedirectionOptions>(options => options.HttpsPort = 443);
                    services.PostConfigure<JwtBearerOptions>("JwtBearer", _ =>
                        throw new InvalidOperationException("Liveness must not initialize JWT authentication."));
                }));
            using var client = livenessFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri(baseAddress),
                AllowAutoRedirect = false
            });

            using var response = await client.GetAsync("/health", cancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(cancellationToken));
        }
    }
}
