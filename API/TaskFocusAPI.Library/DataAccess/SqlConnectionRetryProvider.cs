using Microsoft.Data.SqlClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TaskFocusAPI.Library.DataAccess
{
    internal sealed class SqlConnectionRetryProvider : SqlRetryLogicBaseProvider
    {
        internal static readonly SqlConnectionRetryProvider Instance = new(TimeProvider.System);
        // No new attempt starts after 30 seconds, and the per-attempt cap below ends one open within about 45 seconds,
        // so a request that opens both databases (sign-up) still answers inside the 100-second HTTP timeout released
        // clients use. That covers brief outages such as reconfiguration, not a paused serverless database's resume.
        // A property, not a field: Instance above is initialized first and must not see an unset budget.
        internal static TimeSpan DefaultRetryBudget => TimeSpan.FromSeconds(30);
        private const int MaxConnectTimeout = 15;
        private readonly TimeProvider _clock;
        private readonly Predicate<Exception> _isTransient;
        private readonly TimeSpan _retryBudget;

        internal TimeSpan RetryBudget => _retryBudget;

        internal SqlConnectionRetryProvider(TimeProvider clock, Predicate<Exception> isTransient = null, TimeSpan? retryBudget = null)
        {
            _clock = clock;
            _retryBudget = retryBudget ?? DefaultRetryBudget;
            RetryLogic = SqlConfigurableRetryFactory.CreateExponentialRetryProvider(new SqlRetryLogicOption
            {
                NumberOfTries = 7,
                DeltaTime = TimeSpan.FromSeconds(3),
                MaxTimeInterval = TimeSpan.FromSeconds(14)
            }).RetryLogic;
            var standard = RetryLogic.TransientPredicate;
            _isTransient = isTransient ?? (ex => standard(ex) || IsConnectTimeout(ex));
        }

        // A database resuming from pause can stall the login until the connection timeout (error -2) instead of
        // refusing it with 40613. SqlClient's default list leaves -2 out because a command may have run; this
        // provider only retries opening a connection, so it is safe here.
        internal static bool IsConnectTimeout(Exception ex) => ex is SqlException { Number: -2 };

        internal void Configure(SqlConnection connection)
        {
            var settings = new SqlConnectionStringBuilder(connection.ConnectionString);
            if (settings.ConnectTimeout == 0 || settings.ConnectTimeout > MaxConnectTimeout)
            {
                settings.ConnectTimeout = MaxConnectTimeout;
                connection.ConnectionString = settings.ConnectionString;
            }
            connection.RetryLogicProvider = this;
        }

        public override TResult Execute<TResult>(object sender, Func<TResult> function)
            => ExecuteAsync(sender, () => Task.FromResult(function())).GetAwaiter().GetResult();

        public override Task ExecuteAsync(object sender, Func<Task> function, CancellationToken cancellationToken = default)
            => ExecuteAsync(sender, async () =>
            {
                await function().ConfigureAwait(false);
                return true;
            }, cancellationToken);

        public override async Task<TResult> ExecuteAsync<TResult>(
            object sender, Func<Task<TResult>> function, CancellationToken cancellationToken = default)
        {
            var start = _clock.GetTimestamp();
            SqlRetryLogicBase retry = null;
            while (true)
            {
                try
                {
                    return await function().ConfigureAwait(false);
                }
                catch (Exception ex) when (_isTransient(ex))
                {
                    retry ??= (SqlRetryLogicBase)RetryLogic.Clone();
                    // SqlClient's cloned enumerators share the original random generator.
                    TimeSpan delay;
                    lock (RetryLogic)
                    {
                        if (!retry.TryNextInterval(out delay))
                        {
                            throw;
                        }
                    }
                    if (_clock.GetElapsedTime(start) + delay >= _retryBudget)
                    {
                        throw;
                    }

                    await Task.Delay(delay, _clock, cancellationToken).ConfigureAwait(false);
                    if (_clock.GetElapsedTime(start) >= _retryBudget)
                    {
                        throw;
                    }
                }
            }
        }
    }
}
