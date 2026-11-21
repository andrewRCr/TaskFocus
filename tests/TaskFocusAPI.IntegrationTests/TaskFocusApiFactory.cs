using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TaskFocusAPI.Library.Utilities;

namespace TaskFocusAPI.IntegrationTests
{
    internal sealed class TaskFocusApiFactory(string authConnection, string dataConnection)
        : WebApplicationFactory<Program>
    {
        internal const string AppUrl = "https://taskfocus.example.invalid";

        internal CapturingEmailSender Emails { get; } = new();

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Program reads DefaultConnection before building the host.
            builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = authConnection,
                    ["ConnectionStrings:TaskFocusData"] = dataConnection,
                    ["JwtSecurityKey"] = "taskfocus-integration-test-signing-key-only",
                    ["AzureKeyVaultUrl"] = "https://taskfocus-vault.example.invalid",
                    ["AppUrl"] = AppUrl
                }));

            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(Emails);
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
            });
        }
    }
}
