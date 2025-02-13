using Microsoft.Extensions.Logging;

namespace TaskFocusUI.Library.Logging
{
    public class CustomLoggerConfiguration
    {
        public int EventId {  get; set; }
        public LogLevel ConsoleMinLogLevel { get; set; }
        public LogLevel InMemoryMinLogLevel { get; set; }
    }
}
