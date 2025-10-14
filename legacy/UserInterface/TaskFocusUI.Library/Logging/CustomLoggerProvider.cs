using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace TaskFocusUI.Library.Logging
{
    public class CustomLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, CustomLogger> _loggers =
            new ConcurrentDictionary<string, CustomLogger>();

        public CustomLoggerConfiguration Config { get; private set; }

        public InMemoryLog Log { get; }

        public CustomLoggerProvider(CustomLoggerConfiguration config, InMemoryLog log)
        {
            Config = config;
            Log = log;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new CustomLogger(name, Config, Log));
        }

        public void Dispose() => _loggers.Clear();
    }
}
