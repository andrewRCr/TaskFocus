using Microsoft.Data.SqlClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TaskFocusAPI.Library.DataAccess
{
    internal sealed class SqlConnectionRetryProvider : SqlRetryLogicBaseProvider
    {
        internal static readonly SqlConnectionRetryProvider Instance = new(TimeProvider.System);
        private static readonly TimeSpan RetryBudget = TimeSpan.FromSeconds(30);
        private const int MaxConnectTimeout = 15;
        private readonly TimeProvider _clock;
        private readonly Predicate<Exception> _isTransient;

        internal SqlConnectionRetryProvider(TimeProvider clock, Predicate<Exception> isTransient = null)
        {
            _clock = clock;
            RetryLogic = SqlConfigurableRetryFactory.CreateExponentialRetryProvider(new SqlRetryLogicOption
            {
                NumberOfTries = 7,
                DeltaTime = TimeSpan.FromSeconds(3),
                MaxTimeInterval = TimeSpan.FromSeconds(14)
            }).RetryLogic;
            _isTransient = isTransient ?? RetryLogic.TransientPredicate;
        }

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
                    if (_clock.GetElapsedTime(start) + delay >= RetryBudget)
                    {
                        throw;
                    }

                    await Task.Delay(delay, _clock, cancellationToken).ConfigureAwait(false);
                    if (_clock.GetElapsedTime(start) >= RetryBudget)
                    {
                        throw;
                    }
                }
            }
        }
    }
}
