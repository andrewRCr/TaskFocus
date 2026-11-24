using Microsoft.Data.SqlClient;
using TaskFocusAPI.Library.DataAccess;

namespace TaskFocusAPI.IntegrationTests
{
    [Collection(ApiCollection.Name)]
    public sealed class SqlLoginFailureTests(ApiFixture fixture)
    {
        [Theory]
        [InlineData("Password", "wrong-password", 18456)]
        [InlineData("Trust Server Certificate", false, -2146893019)]
        public async Task ServerLoginFailuresAreNotRetried(string keyword, object value, int number)
        {
            using var connection = fixture.CreateDataConnection();
            var settings = new SqlConnectionStringBuilder(connection.ConnectionString)
            {
                Encrypt = SqlConnectionEncryptOption.Mandatory,
                Pooling = false
            };
            settings[keyword] = value;
            connection.ConnectionString = settings.ConnectionString;
            var clock = new SqlRetryConfigurationTests.RetryClock();
            new SqlConnectionRetryProvider(clock).Configure(connection);

            var error = await Assert.ThrowsAsync<SqlException>(
                () => connection.OpenAsync(TestContext.Current.CancellationToken));

            Assert.Equal(number, error.Number);
            Assert.Empty(clock.Waits);
        }
    }
}
