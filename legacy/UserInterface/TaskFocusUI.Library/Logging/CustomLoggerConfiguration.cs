using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace TaskFocusUI.Library.Logging
{
    public class CustomLoggerConfiguration
    {
        public int EventId {  get; set; }
        public LogLevel ConsoleMinLogLevel { get; set; }
        public LogLevel InMemoryMinLogLevel { get; set; }

        public Dictionary<LogLevel, LogFormat> LogLevels { get; set; } =
            new()
            {
                [LogLevel.Information] = LogFormat.Short,
                [LogLevel.Warning] = LogFormat.Short,
                [LogLevel.Error] = LogFormat.Long
            };

        public enum LogFormat
        {
            Short,
            Long
        }
    }
}
