using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFocusAPI.Data;
using TaskFocusAPI.Library.DataAccess;

namespace TaskFocusAPI.IntegrationTests
{
    public sealed class SqlRetryConfigurationTests
    {
        [Theory]
        [InlineData(60, 15)]
        [InlineData(0, 15)]
        [InlineData(4, 4)]
        public async Task IdentityAndDapperShareBoundedConnectionRetries(int configuredTimeout, int expectedTimeout)
        {
            string connectionString = $"Server=localhost;Connect Timeout={configuredTimeout};ConnectRetryCount=5;ConnectRetryInterval=2";
            await using var factory = new TaskFocusApiFactory(connectionString, connectionString);
            await using var scope = factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var dataAccess = Assert.IsType<SqlDataAccess>(scope.ServiceProvider.GetRequiredService<ISqlDataAccess>());
            var identity = Assert.IsType<SqlConnection>(context.Database.GetDbConnection());
            using var data = dataAccess.CreateConnection("TaskFocusData");

            Assert.IsType<SqlConnectionRetryProvider>(identity.RetryLogicProvider);
            Assert.Same(identity.RetryLogicProvider, data.RetryLogicProvider);
            Assert.False(context.Database.CreateExecutionStrategy().RetriesOnFailure);

            foreach (var connection in new[] { identity, data })
            {
                Assert.Equal(expectedTimeout, connection.ConnectionTimeout);
                var settings = new SqlConnectionStringBuilder(connection.ConnectionString);
                Assert.Equal(5, settings.ConnectRetryCount);
                Assert.Equal(2, settings.ConnectRetryInterval);
                using var command = connection.CreateCommand();
                Assert.Equal(1, command.RetryLogicProvider.RetryLogic.NumberOfTries);
            }
        }

        [Fact]
        public async Task SlowConnectionAttemptsCountAgainstTheRetryBudget()
        {
            var clock = new RetryClock();
            var failure = new TimeoutException();
            var provider = new SqlConnectionRetryProvider(clock, ex => ex == failure);
            var attempts = new List<TimeSpan>();

            var actual = await Assert.ThrowsAsync<TimeoutException>(() => provider.ExecuteAsync(this, () =>
            {
                attempts.Add(clock.Elapsed);
                // The first and last opens use up their connection timeout. The middle one fails immediately.
                if (attempts.Count != 2)
                {
                    clock.Advance(TimeSpan.FromSeconds(15));
                }
                return Task.FromException(failure);
            }, TestContext.Current.CancellationToken));

            Assert.Same(failure, actual);
            Assert.Collection(clock.Waits,
                first => Assert.InRange(first.TotalSeconds, 2.4, 3.6),
                second => Assert.InRange(second.TotalSeconds, 7.2, 10.8));
            var secondAttempt = TimeSpan.FromSeconds(15) + clock.Waits[0];
            var thirdAttempt = secondAttempt + clock.Waits[1];
            Assert.Equal(new[] { TimeSpan.Zero, secondAttempt, thirdAttempt }, attempts);
            Assert.Equal(thirdAttempt + TimeSpan.FromSeconds(15), clock.Elapsed);
        }

        [Theory]
        [InlineData(28, 0, 0)]
        [InlineData(25, 5, 1)]
        public void RetryCannotStartAtOrAfterTheDeadline(int firstAttemptSeconds, int timerOverrunSeconds, int expectedWaits)
        {
            var clock = new RetryClock { TimerOverrun = TimeSpan.FromSeconds(timerOverrunSeconds) };
            var failure = new TimeoutException();
            var provider = new SqlConnectionRetryProvider(clock, ex => ex == failure);
            int attempts = 0;

            Assert.Same(failure, Assert.Throws<TimeoutException>(() => provider.Execute<int>(this, () =>
            {
                attempts++;
                clock.Advance(TimeSpan.FromSeconds(firstAttemptSeconds));
                throw failure;
            })));

            Assert.Equal(1, attempts);
            Assert.Equal(expectedWaits, clock.Waits.Count);
        }

        private sealed class RetryClock : TimeProvider
        {
            public TimeSpan Elapsed { get; private set; }
            public TimeSpan TimerOverrun { get; init; }
            public List<TimeSpan> Waits { get; } = new();
            public override long TimestampFrequency => TimeSpan.TicksPerSecond;
            public override long GetTimestamp() => Elapsed.Ticks;

            public void Advance(TimeSpan time) => Elapsed += time;

            public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
            {
                Waits.Add(dueTime);
                Advance(dueTime + TimerOverrun);
                callback(state);
                return new CompletedTimer();
            }

            private sealed class CompletedTimer : ITimer
            {
                public bool Change(TimeSpan dueTime, TimeSpan period) => false;
                public void Dispose() { }
                public ValueTask DisposeAsync() => ValueTask.CompletedTask;
            }
        }
    }
}
