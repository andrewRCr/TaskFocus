using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using static TaskFocusUI.Library.Logging.CustomLoggerConfiguration;

namespace TaskFocusUI.Library.Logging
{
    public class CustomLogger : ILogger
    {
        private readonly string _name;
        private readonly CustomLoggerConfiguration _config;
        public InMemoryLog Memory { get; }

        public CustomLogger(string name, CustomLoggerConfiguration config, InMemoryLog memory)
        {
            _name = name;
            _config = config;
            Memory = memory;
        }

        IDisposable ILogger.BeginScope<TState>(TState state) => default!;

        private string GetTopLevelString(string nameStr)
        {
            string[] values = nameStr.ToString()!.Split('.');
            return values[values.Length - 1];
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _config.ConsoleMinLogLevel;
        }

        public void Log<TState>(LogLevel logLevel,
                                EventId eventId,
                                TState state,
                                Exception? exception,
                                Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))  return; 
            
            if (_config.EventId == 0 || _config.EventId == eventId.Id)
            {
                // console logging (both desktopUI + webUI)
                switch (_config.LogLevels[logLevel])
                {
                    case LogFormat.Short:
                            Debug.WriteLine($"{GetTopLevelString(_name)}: {DateTime.Now.ToString("HH:mm:ss")} {formatter(state, exception)}");
                        break;
                    case LogFormat.Long:
                            Debug.WriteLine($"[{eventId.Id,2}: {logLevel,-12}] {_name} {DateTime.Now.ToString("HH:mm:ss")} - {formatter(state, exception)}");
                        break;
                    default:
                        break;
                }
             
                // in-memory logging
                if (logLevel >= _config.InMemoryMinLogLevel)             
                    Memory.LogItem($"[{eventId.Id,2}: {logLevel,-12}] {_name} - {formatter(state, exception)}");              
            }
            
        }
    }
}
