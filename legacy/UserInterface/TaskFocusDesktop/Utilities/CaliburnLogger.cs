using Caliburn.Micro;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using TaskFocusUI.Library.Logging;

namespace TaskFocusUI.Library
{
    public class CaliburnLogger : ILog
    {
        private readonly Type _type;
        private readonly CustomLoggerConfiguration _config;

        private readonly List<string> _ignoredInfoStrs = new List<string> {
            "Applied", "availability" };

        public CaliburnLogger(Type type, CustomLoggerConfiguration config)
        {
            _type = type;
            _config = config;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _config.ConsoleMinLogLevel;
        }

        // for caliburn micro
        private string CreateLogMessage(string format, params object[] args)
        {
            return string.Format("[{0}] {1}",
            DateTime.Now.ToString("HH:mm:ss"),
            string.Format(format, args));
        }

        public void Info(string format, params object[] args)
        {
            foreach (var str in _ignoredInfoStrs) if (format.Contains(str)) return; 
            Debug.WriteLine(CreateLogMessage(format, args), "Desktop UI - INFO");
        }

        public void Warn(string format, params object[] args)
        {
            Debug.WriteLine(CreateLogMessage(format, args), "Desktop UI - WARN");
        }

        public void Error(Exception exception)
        {
            Debug.WriteLine(CreateLogMessage(exception.ToString()), "Desktop UI - ERROR");
        }
    }
}
