using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace TaskFocusUI.Library.Logging
{
    public class CustomLoggerConfiguration
    {
        public int EventId {  get; set; }
        public LogLevel ConsoleMinLogLevel { get; set; }
        public LogLevel InMemoryMinLogLevel { get; set; }
    }
}
