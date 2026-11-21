using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
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
        internal const string FromAddress = "account-mail@example.invalid";
        internal const string FromName = "TaskFocus Integration";

        internal CapturingEmailSender Emails => Services.GetRequiredService<CapturingEmailSender>();
        internal CapturingTokenProvider Tokens => Services.GetRequiredService<CapturingTokenProvider>();

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Program reads DefaultConnection before building the host.
            builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = authConnection,
                    ["ConnectionStrings:TaskFocusData"] = dataConnection,
                    ["JwtSecurityKey"] = "taskfocus-integration-test-signing-key-only",
                    ["AzureKeyVaultUrl"] = "https://taskfocus-vault.example.invalid"
                }));

            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Production);
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["AppUrl"] = AppUrl,
                    ["Email:FromAddress"] = FromAddress,
                    ["Email:FromName"] = FromName
                }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<CapturingEmailSender>();
                services.AddSingleton<IEmailSender>(provider => provider.GetRequiredService<CapturingEmailSender>());
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
                services.AddSingleton<CapturingTokenProvider>();
                services.Configure<IdentityOptions>(options => options.Tokens.ProviderMap[TokenOptions.DefaultProvider] =
                    new TokenProviderDescriptor(typeof(CapturingTokenProvider)));
            });
        }
    }
}
