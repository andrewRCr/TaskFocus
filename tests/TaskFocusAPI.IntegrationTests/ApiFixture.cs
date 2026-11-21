using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFocusAPI.Data;
using TaskFocusAPI.Library.DataAccess;
using TaskFocusAPI.Library.Models;
using Testcontainers.MsSql;

namespace TaskFocusAPI.IntegrationTests
{
    [CollectionDefinition(Name)]
    public sealed class ApiCollection : ICollectionFixture<ApiFixture>
    {
        public const string Name = "TaskFocus API with SQL Server";
    }

    public sealed class ApiFixture : IAsyncLifetime
    {
        internal const string Password = "Fixture-password!123";

        private MsSqlContainer? _sql;
        private TaskFocusApiFactory? _factory;

        internal CapturingEmailSender Emails => _factory!.Emails;

        public async ValueTask InitializeAsync()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            const long memoryBytes = 2560L * 1024 * 1024;
            _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
                .WithPassword($"Sql-{Guid.NewGuid():N}-a1!")
                .WithEnvironment("MSSQL_MEMORY_LIMIT_MB", "2048")
                .WithCreateParameterModifier(parameters =>
                {
                    parameters.HostConfig ??= new();
                    parameters.HostConfig.Memory = memoryBytes;
                    parameters.HostConfig.MemorySwap = memoryBytes;
                })
                .Build();

            using (var startup = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                startup.CancelAfter(TimeSpan.FromMinutes(5));
                try
                {
                    await _sql.StartAsync(startup.Token);
                }
                catch (OperationCanceledException ex) when (startup.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException("The SQL Server test container did not start within five minutes.", ex);
                }
            }
            string serverConnection = _sql.GetConnectionString();
            await SqlSchema.CreateDatabasesAsync(serverConnection, cancellationToken);
            string authConnection = DatabaseConnection(serverConnection, "TaskFocusApiAuthDb");
            string dataConnection = DatabaseConnection(serverConnection, "TaskFocusData");
            await SqlSchema.ApplyDataScriptsAsync(dataConnection, cancellationToken);

            _factory = new TaskFocusApiFactory(authConnection, dataConnection);
            await using var scope = _factory.Services.CreateAsyncScope();
            var authDatabase = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database;
            var expectedDatabase = new SqlConnectionStringBuilder(authConnection);
            var configuredDatabase = authDatabase.GetDbConnection();
            if (configuredDatabase.DataSource != expectedDatabase.DataSource ||
                configuredDatabase.Database != expectedDatabase.InitialCatalog)
            {
                throw new InvalidOperationException("The API fixture must migrate only the container's auth database.");
            }

            await authDatabase.MigrateAsync(cancellationToken);
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            EnsureIdentitySuccess(await roles.CreateAsync(new IdentityRole("User")));
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_factory != null)
                {
                    await _factory.DisposeAsync();
                }
            }
            finally
            {
                if (_sql != null)
                {
                    await _sql.DisposeAsync();
                }
            }
        }

        internal HttpClient CreateClient()
        {
            return _factory!.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
        }

        internal SqlConnection CreateDataConnection()
        {
            return new SqlConnection(DatabaseConnection(_sql!.GetConnectionString(), "TaskFocusData"));
        }

        internal async Task<IdentityUser> CreateUserAsync(bool confirmed = true)
        {
            await using var scope = _factory!.Services.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            string email = NewEmail();
            var user = new IdentityUser(email) { Email = email, EmailConfirmed = confirmed };

            EnsureIdentitySuccess(await users.CreateAsync(user, Password));
            scope.ServiceProvider.GetRequiredService<IUserData>().CreateUser(new UserModel
            {
                Id = user.Id,
                Email = email,
                FirstName = "Test",
                LastName = "User"
            });
            EnsureIdentitySuccess(await users.AddToRoleAsync(user, "User"));
            return user;
        }

        internal async Task<HttpClient> CreateAuthenticatedClientAsync(IdentityUser? user = null)
        {
            user ??= await CreateUserAsync();
            var client = CreateClient();
            try
            {
                using var response = await RequestTokenAsync(client, user.Email!, Password);
                response.EnsureSuccessStatusCode();
                var token = await response.Content.ReadFromJsonAsync<TokenResponse>(TestContext.Current.CancellationToken);
                Assert.False(string.IsNullOrWhiteSpace(token?.AccessToken));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
                return client;
            }
            catch
            {
                client.Dispose();
                throw;
            }
        }

        internal static async Task<HttpResponseMessage> RequestTokenAsync(HttpClient client, string email, string password)
        {
            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = email,
                ["password"] = password,
                ["grant_type"] = "password"
            });
            return await client.PostAsync("/token", form, TestContext.Current.CancellationToken);
        }

        internal static string NewEmail() => $"test-{Guid.NewGuid():N}@example.invalid";

        private static string DatabaseConnection(string serverConnection, string database)
        {
            return new SqlConnectionStringBuilder(serverConnection) { InitialCatalog = database }.ConnectionString;
        }

        private static void EnsureIdentitySuccess(IdentityResult result)
        {
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(error => error.Description)));
            }
        }

        internal sealed record TokenResponse(string AccessToken, string UserName);
    }
}
