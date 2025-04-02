using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Data.Utilities;

namespace TaskFocusUI.Library.Data.Services
{
    public partial class DataService : IDataService, IDataServiceInternal
    {
        private IAPIHelper _apiHelper;
        private ILogger<DataService> _logger;
        private ITaskEndpoint _taskEndpoint;
        private IProjectEndpoint _projectEndpoint;
        private IContextEndpoint _contextEndpoint;
        private IUserEndpoint _userEndpoint;
        private IMapper _mapper;
        private IDataHelper _dataHelper;
        private IDataState _dataState;

        public DataService(IAPIHelper apiHelper, ILogger<DataService> logger, ITaskEndpoint taskEndpoint, IProjectEndpoint projectEndpoint,
            IContextEndpoint contextEndpoint, IUserEndpoint userEndpoint, IMapper mapper, IDataHelper dataHelper, IDataState dataState)
        {
            _apiHelper = apiHelper;
            _logger = logger;
            _taskEndpoint = taskEndpoint;
            _projectEndpoint = projectEndpoint;
            _contextEndpoint = contextEndpoint;
            _userEndpoint = userEndpoint;
            _mapper = mapper;
            _dataHelper = dataHelper;
            _dataState = dataState;
        }

        // helper methods
        // ====================

        // wrapper for info logging in either client
        private void LogInformation(string message)
        {
            if (_logger != null) { _logger.LogInformation(message); }
            else { Debug.WriteLine($"DesktopUI - INFO: {message}"); }
        }

        // wrapper for info logging in either client
        private void LogError(string message)
        {
            if (_logger != null) { _logger.LogError(message); }
            else { Debug.WriteLine($"DesktopUI - ERROR: {message}"); }
        }

        // makes working copy clones of new data state
        void IDataServiceInternal.UpdateAllWorkingDataAfterPull()
        {
            UpdateWorkingTasksFromDataState();
            UpdateWorkingProjectsFromDataState();
        }

        // for use when removing a project/context
        void IDataServiceInternal.ShiftCollectionOrderIndices<T>(T collectionDisplayModel, List<T> collectionSource)
        {
            int? previouslyAssignedCollectionIndex = collectionDisplayModel.OrderIndex;

            foreach (T item in collectionSource)
            {
                bool shiftNeeded = item.OrderIndex > previouslyAssignedCollectionIndex;
                if (shiftNeeded) { item.OrderIndex--; }
            }
        }

        // for handling manual sync request events
        public event EventHandler<string>? SyncRequestHandler;

        // for invoking manual sync request events
        public void InvokeSyncRequest(string sourceName)
        {
            SyncRequestHandler?.Invoke(this, sourceName);
        }

        // for checking if complete data state has been loaded
        public bool IsDataStateLoaded() => _dataState.IsDataLoaded();

        // data state CRUD operations
        // ====================

        // for populating local data state
        public async Task FetchAllRemoteData()
        {
            try
            {
                await FetchRemoteUserData();
                await FetchRemoteSettingsData();
                await FetchRemoteTaskData();
                await FetchRemoteProjectData();
                await FetchRemoteContextData();
                LogInformation("FetchAllRemoteData call processed successfully.");

            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                throw;
            }
        }
    }
}
