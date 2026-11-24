using Microsoft.Data.SqlClient;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TaskFocusAPI.Library.DataAccess
{
    internal sealed class SqlConnectionRetryProvider : SqlRetryLogicBaseProvider
    {
        internal static readonly SqlConnectionRetryProvider Instance = new(TimeProvider.System);
        // Each connection open has 30 seconds to start another attempt. An attempt can finish after the deadline,
        // and requests with multiple opens can exceed the client's 100-second HTTP timeout.
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
            _isTransient = isTransient ?? (ex => standard(ex) || IsConnectTimeout(ex) || IsLoginTransportFailure(ex));
        }

        // A database resuming from pause can stall the login until the connection timeout (error -2) instead of
        // refusing it with 40613. SqlClient's default list leaves -2 out because a command may have run; this
        // provider only retries opening a connection, so it is safe here.
        internal static bool IsConnectTimeout(Exception ex) => ex is SqlException { Number: -2 };

        private static bool IsLoginTransportFailure(Exception ex)
        {
            // Retrying a connection open cannot replay a command, even when SqlClient reconnects an idle session.
            if (ex is not SqlException { Number: 0, Class: 20, Errors.Count: 1 } sql)
            {
                return false;
            }

            for (var cause = sql.InnerException; cause != null; cause = cause.InnerException)
            {
                // SqlClient can report a refused TCP connect as SocketError.Success on Linux.
                if (cause is not (IOException or SocketException) ||
                    cause is SocketException { SocketErrorCode: SocketError.Success or SocketError.ConnectionRefused })
                {
                    return false;
                }
            }

            return true;
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
