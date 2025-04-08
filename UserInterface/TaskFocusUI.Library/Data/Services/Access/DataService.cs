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
        private IAPIHelper _apiHelper;

        public DataService(IMapper mapper,
                           IDataHelper dataHelper,
                           IDataState dataState,
                           IUserEndpoint userEndpoint,
                           ITaskEndpoint taskEndpoint,
                           IProjectEndpoint projectEndpoint,
                           IContextEndpoint contextEndpoint,
                           ILogger<DataService> logger,
                           IAPIHelper apiHelper) : base(mapper, dataHelper, dataState, userEndpoint, taskEndpoint, projectEndpoint, contextEndpoint)
        {

            _mapper = mapper;
            _dataHelper = dataHelper;
            _dataState = dataState;
            _userEndpoint = userEndpoint;
            _taskEndpoint = taskEndpoint;
            _projectEndpoint = projectEndpoint;
            _contextEndpoint = contextEndpoint;

            _logger = logger;
            _apiHelper = apiHelper;
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
        public event EventHandler<string>? PreLogoutSyncRequestHandler;

        // for invoking manual sync request events
        public void InvokeSyncRequest(string sourceName, bool isPreLogoutSync = false)
        {
            if (isPreLogoutSync) PreLogoutSyncRequestHandler?.Invoke(this, sourceName);
            else SyncRequestHandler?.Invoke(this, sourceName);
        }

        public void ResetDataStatePreLogoutSyncCompletionFlag() => _dataState.PreLogoutSyncCompleted = false;

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
