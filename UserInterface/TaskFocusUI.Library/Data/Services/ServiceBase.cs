using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Data.Utilities;

namespace TaskFocusUI.Library.Data.Services
{
    public class ServiceBase
    {
        protected ILogger<ServiceBase>? _logger;
        protected IMapper _mapper;
        protected IDataHelper _dataHelper;
        protected IDataState _dataState;

        protected IUserEndpoint _userEndpoint;
        protected ITaskEndpoint _taskEndpoint;
        protected IProjectEndpoint _projectEndpoint;
        protected IContextEndpoint _contextEndpoint;

        public ServiceBase(IMapper mapper,
                           IDataHelper dataHelper,
                           IDataState dataState,
                           IUserEndpoint userEndpoint,
                           ITaskEndpoint taskEndpoint,
                           IProjectEndpoint projectEndpoint,
                           IContextEndpoint contextEndpoint)
        {
            _mapper = mapper;
            _dataHelper = dataHelper;
            _dataState = dataState;

            _userEndpoint = userEndpoint;
            _taskEndpoint = taskEndpoint;
            _projectEndpoint = projectEndpoint;
            _contextEndpoint = contextEndpoint;
        }

        // helper methods
        // ====================

        // class library UI-independent logging
        // wrapper/alt for logging in either client
        private void CreateLogMessage(string message, string logLevel)
        {
            if (_logger != null) _logger.LogInformation($"{"WebUI -"} [{GetTimestamp()}] {string.Format(message)}");         
            else  Debug.WriteLine($"DesktopUI - {GetTopLevelString(this.ToString()!)} - {logLevel}: [{GetTimestamp()}] {message}");          
        }

        protected string GetTopLevelString(string thisStr)
        {
            string[] values = thisStr.ToString()!.Split('.');
            return values[values.Length - 1];
        }

        protected string GetTimestamp()
        {
            int hour = ((DateTime.Now.Hour + 11) % 12) + 1;
            return string.Format("{0}:{1}", hour.ToString(), DateTime.Now.ToString("mm:ss"));
        }

        protected void LogInformation(string message) => CreateLogMessage(message, "INFO");
        protected void LogError(string message) => CreateLogMessage(message, "ERROR");
    }
}
