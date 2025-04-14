using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Data.Utilities;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Services.Synchronization
{
    public class DataSyncService : ServiceBase, IDataSyncService
    {
        private IDataService _dataService;
        private bool _syncInProgress = false;

        private UserDisplayModel? _pushedUserData;
        private UserSettingsDisplayModel? _pushedUserSettingsData;
        private List<TaskModel> _pushedTaskData = [];
        private List<ProjectModel> _pushedProjectData = [];
        private List<ContextModel> _pushedContextData = [];

        public DataSyncService(IMapper mapper,
                               IDataHelper dataHelper,
                               IDataState dataState,
                               IUserEndpoint userEndpoint,
                               ITaskEndpoint taskEndpoint,
                               IProjectEndpoint projectEndpoint,
                               IContextEndpoint contextEndpoint,
                               ILogger<DataSyncService> logger,                          
                               IDataService dataService) : base(mapper, dataHelper, dataState, userEndpoint, taskEndpoint, projectEndpoint, contextEndpoint)
        {

            _mapper = mapper;
            _dataHelper = dataHelper;
            _dataState = dataState;
            _userEndpoint = userEndpoint;
            _taskEndpoint = taskEndpoint;
            _projectEndpoint = projectEndpoint;
            _contextEndpoint = contextEndpoint;

            _logger = logger;
            _dataService = dataService;

            _dataService.SyncRequestHandler += DataService_SyncRequested;
            _dataService.SyncWithCompletionNotifyRequestHandler += DataService_SyncWithCompletionNotifyRequested;
        }

        // helper methods
        // ====================

        private async void DataService_SyncRequested(object? sender, string e)
        {
            bool success = await TrySync();
            if (success) LogInformation($"{GetTopLevelString(e)}'s requested sync operation has been handled.");
            else { LogError($"{GetTopLevelString(e)}'s requested sync operation failed."); }
        }

        private async void DataService_SyncWithCompletionNotifyRequested(object? sender, string e)
        {
            bool success = await TrySync(true);
            if (success) LogInformation($"{GetTopLevelString(e)}'s requested sync operation with completion notification has been handled.");
            else 
            { 
                LogError($"{GetTopLevelString(e)}'s requested sync operation with completion notification failed.");
                _dataState.SetAppRequestedSyncCompleted(true); // do not block log out / exit
            }
        }

        // attempts to Sync(), and re-attempts if an exception is raised
        public async Task<bool> TrySync(bool notifyOnCompletion = false)
        {
            var numRetryAttempts = 3;
            var retryDelay = 1000;
            bool isInitialSync = _dataState.GetLastSync() == DateTimeOffset.MinValue;

            for (int i = 0; i < numRetryAttempts; i++)
            {
                try
                {
                    // attempt sync; if successful, break loop
                    if (isInitialSync) await InitSync();
                    else await Sync(notifyOnCompletion);

                    break;
                }
                catch (Exception ex)
                {
                    // the operation throws an error - log and reattempt
                    LogError($"Sync retry attempt {i + 1}: Exception : {ex.Message}");
                    if (i == numRetryAttempts - 1) return false;
                    await Task.Delay(retryDelay);
                }
            }
            return true;
        }

        // calls TrySync() on passed interval
        private async Task PeriodicSync(TimeSpan interval, CancellationToken cancellationToken = default)
        {
            using PeriodicTimer timer = new(interval);
            while (true)
            {
                try
                {
                    await timer.WaitForNextTickAsync(cancellationToken);
                    await TrySync();
                }
                catch (Exception ex) { LogError(ex.Message); }
            }
        }

        // on client launch, populate empty DataState + initialize PeriodicSync
        private async Task InitSync()
        {
            try
            {
                await _dataService.FetchAllRemoteData();
                // process any time-relevant changes to task state for next sync
                _dataService.PerformCompletedTaskCleanup();
                _dataState.SetLastSync(DateTimeOffset.Now); // log
                LogInformation("InitSync complete; DataState populated.");

                // initialize periodic sync
                TimeSpan interval = TimeSpan.FromSeconds(60);
                await PeriodicSync(interval);
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                throw;
            }
        }

        // synchronization logic
        // ====================

        // syncs at row level; LastUpdated determines who wins at the server
        private async Task Sync(bool notifyOnCompletion = false)
        {
            if (_syncInProgress) throw new Exception("Sync already in progress; operation aborted.");

            _syncInProgress = true;
            LogInformation($"Starting synchronization... (LastSync prior: {_dataState.GetLastSync()})");

            // process any time-relevant changes to task state
            _dataService.PerformCompletedTaskCleanup();

            // push all client rows locally changed since last sync to the server
            DataSyncResult pushResult = await PushSync();
            // pull all records that have changed since last sync...
            // and update the local client data with any changes (inserts, deletions, updates)
            DataSyncResult pullResult = await PullSync();

            // update "working" copies of local data from newly synced data state + log completion
            _dataService.UpdateAllWorkingDataAfterPull();
            _dataState.SetLastSync(DateTimeOffset.Now);

            LogInformation("Synchronization complete.");
            LogInformation($"PUSH: {pushResult.numRowsInserted} rows inserted, {pushResult.numRowsDeleted} rows deleted, {pushResult.numRowsUpdated} rows updated.");
            LogInformation($"PULL: {pullResult.numRowsInserted} rows inserted, {pullResult.numRowsDeleted} rows deleted, {pullResult.numRowsUpdated} rows updated.");
            _syncInProgress = false;

            // notify for post-sync action in client app, if was requested
            if (notifyOnCompletion) _dataState.SetAppRequestedSyncCompleted(true); 
        }

        // PUSHSYNC(): either insert or update any server data that has been changed locally since last sync.
        // we don't do any other update-triggered data processing (shifting indices, updating other data dependent on this, etc) -
        // - as that's all handled locally by the DataService, on the DataState, when changes occur.
        // DataSyncService trusts the local data state, only ensuring it's in sync with the server database.
        private async Task<DataSyncResult> PushSync()
        {
            LogInformation("Pushing changes to server...");
            DataSyncResult pushResult = new();

            // USER DATA (name only; email/pw are handled separately from syncable data)
            var changedClientUser = _dataState.GetChangedUserData();
            if (changedClientUser != null) // local changes have occurred
            {
                await PushSyncableUserData(changedClientUser);
                _dataState.SetChangedUserData(null);
            }

            // SETTINGS DATA
            var changedClientSettings = _dataState.GetChangedSettingsData();
            if (changedClientSettings != null) // local changes have occurred
            {
                await PushSyncableUserData(changedClientSettings);
                _dataState.SetChangedSettingsData(null);       
            }

            // TASK DATA
            foreach (var clientTask in _dataState.GetChangedTaskData())
            {
                DataSyncResult clientTaskResult = await PushSyncableData(clientTask);
                pushResult = _dataHelper.CombineSyncResults(pushResult, clientTaskResult);
            }
            _dataState.GetChangedTaskData().Clear();
            _dataState.SetTempTaskId(0);

            // PROJECT DATA
            foreach (var clientProject in _dataState.GetChangedProjectData())
            {
                DataSyncResult clientProjectResult = await PushSyncableData(clientProject);
                pushResult = _dataHelper.CombineSyncResults(pushResult, clientProjectResult);
            }
            _dataState.GetChangedProjectData().Clear();
            _dataState.SetTempProjectId(0);

            // CONTEXT DATA
            foreach (var clientContext in _dataState.GetChangedContextData())
            {
                DataSyncResult clientContextResult = await PushSyncableData(clientContext);
                pushResult = _dataHelper.CombineSyncResults(pushResult, clientContextResult);
            }
            _dataState.GetChangedContextData().Clear();
            _dataState.SetTempContextId(0);

            // finalize, return result
            if (!_dataHelper.SyncChangesDetected(pushResult)) LogInformation("No changes detected on push.");
            LogInformation("Push complete.");
            return pushResult;
        }

        // process push at row level - user data
        private async Task<DataSyncResult> PushSyncableUserData(ISyncableUserData userData)
        {
            DataSyncResult result = new();
            ESyncableUserDataType changedDataType = userData.DataType;
            ISyncableUserData changedClientData = userData;
            ISyncableUserData serverData = changedDataType == ESyncableUserDataType.User ?
                await _userEndpoint.GetCurrentUserData() : await _userEndpoint.GetCurrentUserSettings();

            bool serverWon = false;
            if (serverData.ClientLastUpdated > changedClientData!.ServerLastUpdated)
            {
                // conflict - server data is newer than client
                // server wins; ignore changes and just update time
                serverData.ServerLastUpdated = DateTimeOffset.Now;
                serverData.ClientLastUpdated = changedClientData.ServerLastUpdated;
                serverWon = true;
            }
            else // client data is newer than server
            {
                changedClientData.ServerLastUpdated = DateTimeOffset.Now;
            }

            switch (changedDataType)
            {
                case ESyncableUserDataType.User:

                    UserDisplayModel changedClientUser = (UserDisplayModel)changedClientData;
                    UserDisplayModel serverUser = (UserDisplayModel)serverData;
                    if (serverWon) await _userEndpoint.UpdateName(_mapper.Map<UserModel>(serverUser));                   
                    else
                    {
                        _pushedUserData = changedClientUser;
                        result.numRowsUpdated++;
                    }

                    break;

                case ESyncableUserDataType.Settings:

                    UserSettingsDisplayModel changedClientSettings = (UserSettingsDisplayModel)changedClientData;
                    UserSettingsDisplayModel serverSettings = (UserSettingsDisplayModel)serverData;
                    if (serverWon) await _userEndpoint.UpdateUserSettings(_mapper.Map<UserSettingsModel>(serverSettings));                   
                    else
                    {
                        _pushedUserSettingsData = changedClientSettings;
                        result.numRowsUpdated++;
                    }

                    break;
                default:
                    break;
            }              
           
            return result;
        }

        // process push at row level - task/project/context data
        private async Task<DataSyncResult> PushSyncableData(ISyncableData data)
        {
            DataSyncResult result = new();

            if (data.Id == null && data.Deleted.HasValue)
            {
                // was deleted locally before pushed to server; ignore
                return result;
            }

            if (data.Id == null) // doesn't exist on server; insert
            {
                if (data.ServerLastUpdated == DateTimeOffset.MinValue)
                    data.ServerLastUpdated = DateTimeOffset.Now;

                switch (data.DataType)
                {
                    case ESyncableDataType.Task:

                        TaskModel insertedTask = await _taskEndpoint.AddTask(_mapper.Map<TaskModel>(
                            (TaskDisplayModel)data), _dataState.GetCurrentUser()!.Id);
                        _pushedTaskData.Add(_mapper.Map<TaskModel>((TaskDisplayModel)data));

                        int taskIndex = _dataState.GetTasks()!.FindIndex(x => x.TempLocalId == data.TempLocalId);
                        _dataState.GetTasks()![taskIndex].Id = insertedTask.Id; // update with new server-granted id
                        _dataState.GetTasks()![taskIndex].TempLocalId = null; //

                        break;

                    case ESyncableDataType.Project:

                        ProjectModel insertedProject = await _projectEndpoint.AddProject(
                            _mapper.Map<ProjectModel>((ProjectDisplayModel)data), _dataState.GetCurrentUser()!.Id);
                        _pushedProjectData.Add(_mapper.Map<ProjectModel>((ProjectDisplayModel)data));

                        int projectIndex = _dataState.GetProjects()!.FindIndex(x => x.TempLocalId == data.TempLocalId);
                        _dataState.GetProjects()![projectIndex].Id = insertedProject.Id; // update with new server-granted id
                        _dataState.GetProjects()![projectIndex].TempLocalId = null; //

                        break;

                    case ESyncableDataType.Context:

                        ContextModel insertedContext= await _contextEndpoint.AddContext(_mapper.Map<ContextModel>((ContextDisplayModel)data), _dataState.GetCurrentUser()!.Id);
                        _pushedContextData.Add(_mapper.Map<ContextModel>((ContextDisplayModel)data));

                        int contextIndex = _dataState.GetContexts()!.FindIndex(x => x.TempLocalId == data.TempLocalId);
                        _dataState.GetProjects()![contextIndex].Id = insertedContext.Id; // update with new server-granted id
                        _dataState.GetProjects()![contextIndex].TempLocalId = null; //

                        break;
                }

                result.numRowsInserted++;
            }
            else if (data.Deleted.HasValue) // delete
            {
                switch (data.DataType)
                {
                    case ESyncableDataType.Task:

                        TaskModel serverTask = await _taskEndpoint.GetTaskById((int)data.Id);
                        await _taskEndpoint.DeleteTask(serverTask);

                        break;

                    case ESyncableDataType.Project:

                        ProjectModel serverProject = await _projectEndpoint.GetProjectById((int)data.Id);
                        await _projectEndpoint.DeleteProject(serverProject);
                        
                        break;

                    case ESyncableDataType.Context:

                        ContextModel serverContext = await _contextEndpoint.GetContextById((int) data.Id);
                        await _contextEndpoint.DeleteContext(serverContext);

                        break;
                }
                        
                result.numRowsDeleted++;
            }
            else // update
            {
                switch (data.DataType)
                {
                    case ESyncableDataType.Task:

                        TaskModel serverTask = await _taskEndpoint.GetTaskById((int)data.Id);
                        if (serverTask.ClientLastUpdated > data.ServerLastUpdated)
                        {
                            // conflict - server data is newer than client
                            // server wins; ignore changes and just update time
                            serverTask.ServerLastUpdated = DateTimeOffset.Now;
                            serverTask.ClientLastUpdated = data.ServerLastUpdated;
                            await _taskEndpoint.UpdateTask(serverTask);
                        }
                        else // client data is newer than server
                        {
                            data.ServerLastUpdated = DateTimeOffset.Now;
                            await _taskEndpoint.UpdateTask(_mapper.Map<TaskModel>(data));

                            _pushedTaskData.Add(_mapper.Map<TaskModel>((TaskDisplayModel)data));
                            int index = _dataState.GetTasks()!.FindIndex(x => x.Id == data.Id);
                            if (index != -1) { _dataState.GetTasks()![index] = (TaskDisplayModel)data; }

                            result.numRowsUpdated++;
                        }
                        break;

                    case ESyncableDataType.Project:

                        ProjectModel serverProject = await _projectEndpoint.GetProjectById((int)data.Id);
                        if (serverProject.ClientLastUpdated > data.ServerLastUpdated)
                        {
                            // conflict - server data is newer than client
                            // server wins; ignore changes and just update time
                            serverProject.ServerLastUpdated = DateTimeOffset.Now;
                            serverProject.ClientLastUpdated = data.ServerLastUpdated;
                            await _projectEndpoint.UpdateProject(serverProject);
                        }
                        else // client data is newer than server
                        {
                            data.ServerLastUpdated = DateTimeOffset.Now;
                            await _projectEndpoint.UpdateProject(_mapper.Map<ProjectModel>(data));

                            _pushedProjectData.Add(_mapper.Map<ProjectModel>((ProjectDisplayModel)data));
                            int index = _dataState.GetProjects()!.FindIndex(x => x.Id == data.Id);
                            if (index != -1) { _dataState.GetProjects()![index] = (ProjectDisplayModel)data; }

                            result.numRowsUpdated++;
                        }
                        break;

                    case ESyncableDataType.Context:

                        ContextModel serverContext = await _contextEndpoint.GetContextById((int)data.Id);
                        if (serverContext.ClientLastUpdated > data.ServerLastUpdated)
                        {
                            // conflict - server data is newer than client
                            // server wins; ignore changes and just update time
                            serverContext.ServerLastUpdated = DateTimeOffset.Now;
                            serverContext.ClientLastUpdated = data.ServerLastUpdated;
                            await _contextEndpoint.UpdateContext(serverContext);
                        }
                        else // client data is newer than server
                        {
                            data.ServerLastUpdated = DateTimeOffset.Now;
                            await _contextEndpoint.UpdateContext(_mapper.Map<ContextModel>(data));

                            _pushedContextData.Add(_mapper.Map<ContextModel>((ContextDisplayModel)data));
                            int index = _dataState.GetContexts()!.FindIndex(x => x.Id == data.Id);
                            if (index != -1) { _dataState.GetContexts()![index] = (ContextDisplayModel)data; }

                            result.numRowsUpdated++;
                        }
                        break;
                }
            }

            return result;
        }

        // PULLSYNC(): gets all records that have changed since LastSync
        // updates local client data with any changes (inserts, deletions, updates)
        private async Task<DataSyncResult> PullSync()
        {
            DataSyncResult pullResult = new();

            // USER DATA
            UserModel serverUser = await _userEndpoint.GetCurrentUserData();
            if (serverUser.ServerLastUpdated >= _dataState.GetLastSync()) // remote changes have occurred
            {
                await PullSyncableUserData(serverUser);
            }
            _pushedUserData = null; // no longer needed for reference

            // SETTINGS DATA
            UserSettingsModel serverSettings = await _userEndpoint.GetCurrentUserSettings();
            if (serverSettings.ServerLastUpdated >= _dataState.GetLastSync()) // remote changes have occurred
            {
                await PullSyncableUserData(serverSettings);                 
            }
            _pushedUserSettingsData = null; // no longer needed for reference

            // TASK DATA
            DataSyncResult tasksPullResult = new();
            List<TaskModel> serverTasks = await _taskEndpoint.GetAllTasksForUser();
            var changedRemoteTaskRows = serverTasks.Where(
                x => x.ServerLastUpdated >= _dataState.GetLastSync()).ToList();

            // handle local inserts/updates (originating from another client)
            foreach (var serverTask in changedRemoteTaskRows)
            {
                DataSyncResult serverTaskResult = PullSyncableData(_mapper.Map<TaskDisplayModel>(serverTask));
                tasksPullResult = _dataHelper.CombineSyncResults(tasksPullResult, serverTaskResult);
            }

            // handle local deletions (originating from another client)
            DataSyncResult taskDeletionResult = await PullServerDataDeletions(ESyncableDataType.Task);
            tasksPullResult = _dataHelper.CombineSyncResults(tasksPullResult, taskDeletionResult); 

            // cleanup temp push history
            _pushedTaskData.Clear();

            // trigger UI update if needed + add to total pull result
            if (_dataHelper.SyncChangesDetected(tasksPullResult))
            {
                _dataState.InvokeDataStateChanged(nameof(EDataRefreshType.Tasks));
                pullResult = _dataHelper.CombineSyncResults(pullResult, tasksPullResult);
            }

            // PROJECT DATA
            DataSyncResult projectsPullResult = new();
            List<ProjectModel> serverProjects = await _projectEndpoint.GetAllProjectsForUser();
            var changedRemoteProjectRows = serverProjects.Where(
                x => x.ServerLastUpdated >= _dataState.GetLastSync()).ToList();

            // handle local inserts/updates (originating from another client)
            foreach (var serverProject in changedRemoteProjectRows)
            {
                DataSyncResult serverProjectResult = PullSyncableData(_mapper.Map<ProjectDisplayModel>(serverProject));
                projectsPullResult = _dataHelper.CombineSyncResults(projectsPullResult, serverProjectResult);
            }

            // handle local deletions (originating from another client)
            DataSyncResult projectDeletionResult = await PullServerDataDeletions(ESyncableDataType.Project);
            projectsPullResult = _dataHelper.CombineSyncResults(projectsPullResult, projectDeletionResult);

            // cleanup temp push history
            _pushedProjectData.Clear();

            // trigger UI update if needed + add to total pull result
            if (_dataHelper.SyncChangesDetected(projectsPullResult))
            {
                _dataState.InvokeDataStateChanged(nameof(EDataRefreshType.Projects));
                pullResult = _dataHelper.CombineSyncResults(pullResult, projectsPullResult);
            }

            // CONTEXT DATA
            DataSyncResult contextsPullResult = new();
            List<ContextModel> serverContexts = await _contextEndpoint.GetAllContextsForUser();
            var changedRemoteContextRows = serverContexts.Where(
                x => x.ServerLastUpdated >= _dataState.GetLastSync()).ToList();

            // handle local inserts/updates (originating from another client)
            foreach (var serverContext in changedRemoteContextRows)
            {
                DataSyncResult serverContextResult = PullSyncableData(_mapper.Map<ContextDisplayModel>(serverContext));
                contextsPullResult = _dataHelper.CombineSyncResults(contextsPullResult, serverContextResult);
            }

            // handle local deletions (originating from another client)
            DataSyncResult contextDeletionResult = await PullServerDataDeletions(ESyncableDataType.Context);
            contextsPullResult = _dataHelper.CombineSyncResults(contextsPullResult, contextDeletionResult);

            // cleanup temp push history
            _pushedContextData.Clear();

            // trigger UI update if needed + add to total pull result
            if (_dataHelper.SyncChangesDetected(contextsPullResult))
            {
                _dataState.InvokeDataStateChanged(nameof(EDataRefreshType.Contexts));
                pullResult = _dataHelper.CombineSyncResults(pullResult, contextsPullResult);
            }

            // finalize, return result
            if (!_dataHelper.SyncChangesDetected(pullResult)) LogInformation("No changes detected on pull.");
            LogInformation("Pull complete.");
            return pullResult;
        }

        // process local user data updates based on remote data row 
        private async Task<DataSyncResult> PullSyncableUserData(ISyncableUserData userData)
        {
            DataSyncResult result = new();

            ISyncableUserData serverData = userData.DataType == ESyncableUserDataType.User ?
                await _userEndpoint.GetCurrentUserData() : await _userEndpoint.GetCurrentUserSettings();

            if (serverData.ServerLastUpdated >= _dataState.GetLastSync())
            {
                // only pull if we didn't just push the change
                if (userData.DataType == ESyncableUserDataType.User && _pushedUserData == null ||
                    userData.DataType == ESyncableUserDataType.Settings && _pushedUserSettingsData == null)
                {
                    serverData.ServerLastUpdated = DateTimeOffset.Now;
                    // update local data store
                    switch (userData.DataType)
                    {
                        case ESyncableUserDataType.User:

                            UserModel serverUser = (UserModel)userData;
                            var displayServerUser = _mapper.Map<UserDisplayModel>(serverUser);
                            _dataState.SetCurrentUser(displayServerUser);

                            break;

                        case ESyncableUserDataType.Settings:

                            UserSettingsModel serverSettings = (UserSettingsModel)serverData;
                            var displayServerSettings = _mapper.Map<UserSettingsDisplayModel>(serverData); 
                            _dataState.SetUserSettings(displayServerSettings);

                            break;

                        default:
                            break;
                    }

                    result.numRowsUpdated++;
                }
            }

            return result;
        }

        // process local task/project/context insert/update/deletes based on remote data row
        private DataSyncResult PullSyncableData(ISyncableData data)
        {
            DataSyncResult result = new();

            switch (data.DataType)
            {
                case ESyncableDataType.Task:

                    // do not pull if we just pushed the change
                    var pushedServerTask = _pushedTaskData.Where(x => x.Id == data.Id);
                    if (pushedServerTask.Any()) return result;

                    TaskDisplayModel displayServerTask = (TaskDisplayModel)data;
                    TaskDisplayModel? clientTask = _dataState.GetTasks()!.Where(
                        x => x.Id == displayServerTask.Id).FirstOrDefault();

                    if (clientTask == null) // insert
                    {
                        _dataState.GetTasks()!.Add(displayServerTask.Clone());
                        result.numRowsInserted++;
                    }
                    else // update
                    {
                        int i = _dataState.GetTasks()!.IndexOf(clientTask);
                        _dataState.GetTasks()![i] = displayServerTask;
                        result.numRowsUpdated++;
                    }

                    break;

                case ESyncableDataType.Project:

                    // do not pull if we just pushed the change
                    var pushedServerProject = _pushedProjectData.Where(x => x.Id == data.Id);
                    if (pushedServerProject.Any()) return result; 

                    ProjectDisplayModel displayServerProject = (ProjectDisplayModel)data;
                    ProjectDisplayModel? clientProject = _dataState.GetProjects()!.Where(
                        x => x.Id == displayServerProject.Id).FirstOrDefault();

                    if (clientProject == null) // insert
                    {
                        _dataState.GetProjects()!.Add(displayServerProject.Clone());
                        result.numRowsInserted++;
                    }
                    else // update
                    {
                        int i = _dataState.GetProjects()!.IndexOf(clientProject);
                        _dataState.GetProjects()![i] = displayServerProject;
                        result.numRowsUpdated++;
                    }
                    break;

                case ESyncableDataType.Context:

                    // do not pull if we just pushed the change
                    var pushedServerContext = _pushedContextData.Where(x => x.Id == data.Id);
                    if (pushedServerContext.Any()) return result;

                    ContextDisplayModel displayServerContext = (ContextDisplayModel)data;
                    ContextDisplayModel? clientContext = _dataState.GetContexts()!.Where(
                        x => x.Id == displayServerContext.Id).FirstOrDefault();

                    if (clientContext == null) // insert
                    {
                        _dataState.GetContexts()!.Add(displayServerContext.Clone());
                        result.numRowsInserted++;
                    }
                    else // update
                    {
                        int i = _dataState.GetContexts()!.IndexOf(clientContext);
                        _dataState.GetContexts()![i] = displayServerContext;
                        result.numRowsUpdated++;
                    }
                    break;
                default:
                    break;
            }

            return result;
        }

        // handles local deletions based on remote data type
        private async Task<DataSyncResult> PullServerDataDeletions(ESyncableDataType dataType)
        {
            DataSyncResult pullDeletionsResult = new();

            switch (dataType)
            {
                case ESyncableDataType.Task:

                    List<TaskModel> serverTasks = await _taskEndpoint.GetAllTasksForUser();

                    foreach (var clientDisplayTask in _dataState.GetTasks()!.ToList())
                    {
                        TaskModel? serverTask = serverTasks.Find(x => x.Id == clientDisplayTask.Id);
                        if (serverTask == null) // not found on server? delete locally
                        {
                            _dataService.HandleIndexShiftsOnTaskDeletion(clientDisplayTask);
                            _dataState.GetTasks()!.Remove(clientDisplayTask);
                            pullDeletionsResult.numRowsDeleted++;
                        }
                    }

                    break;

                case ESyncableDataType.Project:

                    List<ProjectModel> serverProjects = await _projectEndpoint.GetAllProjectsForUser();

                    foreach (var clientDisplayProject in _dataState.GetProjects()!.ToList())
                    {
                        ProjectModel? serverProject = serverProjects.Find(x => x.Id == clientDisplayProject.Id);
                        if (serverProject == null) // not found on server? delete locally
                        {
                            // ensure other projects have updated OrderIndex values
                            _dataService.ShiftCollectionOrderIndices(clientDisplayProject, _dataState.GetProjects()!);
                            _dataState.GetProjects()!.Remove(clientDisplayProject);
                            pullDeletionsResult.numRowsDeleted++;
                        }
                    }

                    break;

                case ESyncableDataType.Context:

                    List<ContextModel> serverContexts = await _contextEndpoint.GetAllContextsForUser();

                    foreach (var clientDisplayContext in _dataState.GetContexts()!.ToList())
                    {
                        ContextModel? serverContext = serverContexts.Find(x => x.Id == clientDisplayContext.Id);
                        if (serverContext == null) // not found on server? delete locally
                        {
                            // ensure other contexts have updated OrderIndex values
                            _dataService.ShiftCollectionOrderIndices(clientDisplayContext, _dataState.GetContexts()!);
                            _dataState.GetContexts()!.Remove(clientDisplayContext);
                            pullDeletionsResult.numRowsDeleted++;
                        }
                    }
                    break;

                default:
                    break;
            }

            return pullDeletionsResult;
        }
    }
}
