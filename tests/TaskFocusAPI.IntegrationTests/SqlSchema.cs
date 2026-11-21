using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace TaskFocusAPI.IntegrationTests
{
    internal static class SqlSchema
    {
        private static readonly Regex BatchSeparator = new(
            @"^[^\S\r\n]*GO[^\S\r\n]*(?:--[^\r\n]*)?\r?$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);

        internal static async Task CreateDatabasesAsync(string serverConnection, CancellationToken cancellationToken)
        {
            await using var connection = new SqlConnection(serverConnection);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE DATABASE [TaskFocusApiAuthDb];";
            await command.ExecuteNonQueryAsync(cancellationToken);
            command.CommandText = "CREATE DATABASE [TaskFocusData];";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        internal static async Task ApplyDataScriptsAsync(string connectionString, CancellationToken cancellationToken)
        {
            string scripts = FindSqlScripts();
            string tablesDirectory = Path.Combine(scripts, "Tables");
            // User -> Context -> Project -> Task. UserSettings is independent.
            string[] tableOrder = ["User.sql", "UserSettings.sql", "Context.sql", "Project.sql", "Task.sql"];
            var tableFiles = Directory.GetFiles(tablesDirectory, "*.sql");
            if (!tableOrder.ToHashSet(StringComparer.Ordinal).SetEquals(tableFiles.Select(file => Path.GetFileName(file))))
            {
                throw new InvalidOperationException("Update the table dependency order in SqlSchema for the current SQL files.");
            }

            var procedures = Directory.EnumerateFiles(Path.Combine(scripts, "Stored Procedures"), "*.sql")
                .Order(StringComparer.Ordinal);
            var files = tableOrder.Select(table => Path.Combine(tablesDirectory, table)).Concat(procedures);

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            foreach (string file in files)
            {
                string sql = (await File.ReadAllTextAsync(file, cancellationToken)).TrimStart('\uFEFF');
                foreach (string batch in BatchSeparator.Split(sql).Where(batch => !string.IsNullOrWhiteSpace(batch)))
                {
                    await using var command = connection.CreateCommand();
                    command.CommandText = batch;
                    try
                    {
                        await command.ExecuteNonQueryAsync(cancellationToken);
                    }
                    catch (SqlException exception)
                    {
                        throw new InvalidOperationException($"Could not apply SQL script {Path.GetFileName(file)}.", exception);
                    }
                }
            }
        }

        private static string FindSqlScripts()
        {
            for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                string scripts = Path.Combine(directory.FullName, "Database", "TaskFocusData", "dbo");
                if (Directory.Exists(scripts))
                {
                    return scripts;
                }
            }

            throw new DirectoryNotFoundException("Run the integration tests from a build inside the TaskFocus checkout.");
        }
    }
}
