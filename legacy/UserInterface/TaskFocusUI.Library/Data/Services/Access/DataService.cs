using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Data.Utilities;

namespace TaskFocusUI.Library.Data.Services
{
    public partial class DataService : ServiceBase, IDataService, IDataServiceInternal
    {
        public DataService(IMapper mapper,
                           IDataHelper dataHelper,
                           IDataState dataState,
                           IUserEndpoint userEndpoint,
                           ITaskEndpoint taskEndpoint,
                           IProjectEndpoint projectEndpoint,
                           IContextEndpoint contextEndpoint,
                           ILogger<DataService> logger) : base(mapper, dataHelper, dataState, userEndpoint, taskEndpoint, projectEndpoint, contextEndpoint)
        {

            _mapper = mapper;
            _dataHelper = dataHelper;
            _dataState = dataState;
            _userEndpoint = userEndpoint;
            _taskEndpoint = taskEndpoint;
            _projectEndpoint = projectEndpoint;
            _contextEndpoint = contextEndpoint;

            _logger = logger;
        }

        // helper methods
        // ====================

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
        public event EventHandler<string>? SyncWithCompletionNotifyRequestHandler;

        // for invoking manual sync request events
        public void InvokeSyncRequest(string sourceName, bool notifyOnCompletion = false)
        {
            if (notifyOnCompletion) SyncWithCompletionNotifyRequestHandler?.Invoke(this, sourceName);
            else SyncRequestHandler?.Invoke(this, sourceName);
        }

        // for checking if complete data state has been loaded
        public bool IsDataStateLoaded() => _dataState.IsDataLoaded();

        // for checking if front-end is clear to proceed with some post-sync action (logout, exit)
        public bool IsAppRequestedSyncCompleted() => _dataState.GetAppRequestedSyncCompleted();

        public DateTimeOffset GetDataStateLastSync() => _dataState.GetLastSync();

        public DataSyncResult GetDataStateLastSyncResult() => _dataState.GetLastSyncResult();

        public ESyncStatus GetCurrentSyncStatus() => _dataState.GetCurrentSyncStatus();

        public int GetSyncIntervalSeconds() => _dataState.GetSyncIntervalSeconds();

        public void ResetDataStateOnLogout()
        {
            _dataState.SetCurrentUser(null);
            _dataState.SetWorkingCurrentUser(null);
            _dataState.SetUserSettings(null);
            _dataState.SetWorkingUserSettings(null);
            _dataState.SetTasks(null);
            _dataState.SetWorkingTasks(null);
            _dataState.SetProjects(null);
            _dataState.SetWorkingProjects(null);
            _dataState.SetContexts(null);
            _dataState.SetWorkingContexts(null);
            _dataState.SetAppRequestedSyncCompleted(false);
            _dataState.SetLastSync(DateTimeOffset.MinValue);
        }

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
