using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TaskFocusAPI.Services;

namespace TaskFocusAPI.IntegrationTests
{
    public sealed class JwtValidationTests
    {
        [Fact]
        public Task BearerValidationReturns401DuringVaultOutageCooldownAndRecovers() =>
            AssertSigningKeyRecoveryAsync(() => throw new InvalidOperationException("Simulated Key Vault outage."));

        [Fact]
        public Task BearerValidationRejectsShortKeyAndRecoversAfterCorrection() =>
            AssertSigningKeyRecoveryAsync(() => new string('é', 15) + "a");

        private static async Task AssertSigningKeyRecoveryAsync(Func<string> readInvalidSecret)
        {
            string secret = new string('é', 16); // 32 UTF-8 bytes
            using var configuration = new ConfigurationManager();
            var clock = new TestClock();
            int lookups = 0;
            bool secretCorrected = false;

            await using var factory = new TaskFocusApiFactory(string.Empty, string.Empty);
            await using var retryFactory = factory.WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                {
                    services.AddControllers().AddApplicationPart(typeof(JwtProbeController).Assembly);
                    services.AddSingleton<IJwtKeyProvider>(provider => new JwtKeyProvider(
                        configuration, provider.GetRequiredService<ILogger<JwtKeyProvider>>(), clock, ReadSecret));
                }));
            using var client = retryFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            string token = handler.WriteToken(new JwtSecurityToken(
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            Assert.Equal(HttpStatusCode.Unauthorized, await GetStatusAsync());
            Assert.Equal(1, lookups);

            secretCorrected = true;
            clock.Advance(TimeSpan.FromSeconds(29));
            Assert.All(await SendRequestsAsync(), status => Assert.Equal(HttpStatusCode.Unauthorized, status));
            Assert.Equal(1, lookups);

            clock.Advance(TimeSpan.FromSeconds(1));
            Assert.All(await SendRequestsAsync(), status => Assert.Equal(HttpStatusCode.OK, status));
            Assert.Equal(2, lookups);

            secretCorrected = false;
            clock.Advance(TimeSpan.FromMinutes(1));
            Assert.Equal(HttpStatusCode.OK, await GetStatusAsync());
            Assert.Equal(2, lookups);

            string ReadSecret()
            {
                Interlocked.Increment(ref lookups);
                return secretCorrected ? secret : readInvalidSecret();
            }

            Task<HttpStatusCode[]> SendRequestsAsync() =>
                Task.WhenAll(Enumerable.Range(0, 4).Select(_ => GetStatusAsync()));

            async Task<HttpStatusCode> GetStatusAsync()
            {
                using var response = await client.GetAsync("/jwt-probe", TestContext.Current.CancellationToken);
                return response.StatusCode;
            }
        }

        private sealed class TestClock : TimeProvider
        {
            private long _timestamp;

            public override long TimestampFrequency => TimeSpan.TicksPerSecond;
            public override long GetTimestamp() => _timestamp;
            public void Advance(TimeSpan elapsed) => _timestamp += elapsed.Ticks;
        }
    }

    [Authorize]
    public sealed class JwtProbeController : ControllerBase
    {
        [HttpGet("/jwt-probe")]
        public IActionResult Get() => Ok();
    }
}
