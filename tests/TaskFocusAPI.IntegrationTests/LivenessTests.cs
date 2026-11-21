using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TaskFocusAPI.IntegrationTests
{
    public sealed class LivenessTests
    {
        [Fact]
        public void CheckedInAccountMailDefaultsUseHttpsAndDevSender()
        {
            using var defaults = new ConfigurationManager();
            defaults.SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json");

            Assert.Equal("https://taskfocus.andrewcreekmore.dev", defaults["AppUrl"]);
            Assert.Equal("taskfocus@andrewcreekmore.dev", defaults["Email:FromAddress"]);
            Assert.Equal("TaskFocus", defaults["Email:FromName"]);
        }

        [Theory]
        [InlineData("Production")]
        [InlineData("Staging")]
        public async Task StartupRejectsHttpAppUrlOutsideDevelopment(string environment)
        {
            await using var factory = new TaskFocusApiFactory(string.Empty, string.Empty);
            await using var insecureFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?> { ["AppUrl"] = "http://taskfocus.example.invalid" }));
            });

            var error = Assert.Throws<InvalidOperationException>(() => insecureFactory.CreateClient());
            Assert.Contains("AppUrl must be an absolute HTTPS URL", error.Message);
        }

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
