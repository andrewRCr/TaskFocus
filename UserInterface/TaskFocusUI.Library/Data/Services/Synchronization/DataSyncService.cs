using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

        private UserDisplayModel? _pushedUserData;
        private UserSettingsDisplayModel? _pushedUserSettingsData;
        private List<TaskModel> _pushedTaskData = [];
        private List<ProjectModel> _pushedProjectData = [];
        private List<ContextModel> _pushedContextData = [];
        private readonly HashSet<int> _createdContextIds = [];

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
        public async Task<bool> TrySync(bool notifyOnCompletion = false, Action<Exception>? onFailure = null)
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
                    if (i == numRetryAttempts - 1)
                    {
                        onFailure?.Invoke(ex);
                        return false;
                    }
                    await Task.Delay(retryDelay);
                }
            }
            return true;
        }

        // on client launch, populate empty DataState + initialize PeriodicSync
        private async Task InitSync()
        {
            try
            {
                await _dataService.FetchAllRemoteData();
                ShowQueuedUserData();
                // process any time-relevant changes to task state for next sync
                _dataService.PerformCompletedTaskCleanup();
                _dataService.PerformTodayTaskCleanup();
                _dataState.SetLastSync(DateTimeOffset.Now); // log
                _dataState.SetLastSyncResult(new DataSyncResult());
                _dataState.SetCurrentSyncStatus(ESyncStatus.Synchronized);
                LogInformation("InitSync complete; DataState populated.");

                // The initial load completes here. Periodic sync continues in the background.
                var timer = new PeriodicTimer(TimeSpan.FromSeconds(_dataState.GetSyncIntervalSeconds()));
                _ = PeriodicSync(timer);
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                throw;
            }
        }

        // calls TrySync() on passed interval
        private async Task PeriodicSync(PeriodicTimer timer)
        {
            using (timer)
            {
                while (true)
                {
                    try
                    {
                        await timer.WaitForNextTickAsync();
                        await TrySync();
                    }
                    catch (Exception ex) { LogError(ex.Message); }
                }
            }
        }

        // synchronization logic
        // ====================

        // syncs at row level; LastUpdated determines who wins at the server
        private async Task Sync(bool notifyOnCompletion = false)
        {
            if (_dataState.GetCurrentSyncStatus() == ESyncStatus.SyncInProgress) throw new Exception("Sync already in progress; operation aborted.");

            _dataState.SetCurrentSyncStatus(ESyncStatus.SyncInProgress);
            LogInformation($"Starting synchronization... (LastSync prior: {_dataState.GetLastSync()})");

            // process any time-relevant changes to task state
            _dataService.PerformCompletedTaskCleanup();
            _dataService.PerformTodayTaskCleanup();

            // push all client rows locally changed since last sync to the server
            DataSyncResult pushResult = await PushSync();
            // pull all records that have changed since last sync...
            // and update the local client data with any changes (inserts, deletions, updates)
            DataSyncResult pullResult = await PullSync();
            // then delete expired completed tasks, with the saved delay and the latest rows
            pushResult = _dataHelper.CombineSyncResults(pushResult, await _dataService.DeleteExpiredTasks());

            // update "working" copies of local data from newly synced data state + log completion
            _dataService.UpdateAllWorkingDataAfterPull();
            // clean up again with the settings this sync saved or pulled, so choosing "Immediately" or a shorter delay shows
            // at once (the next sync sends the changes)
            _dataService.PerformCompletedTaskCleanup();
            _dataState.SetLastSync(DateTimeOffset.Now);

            LogInformation("Synchronization complete.");
            LogInformation($"PUSH: {pushResult.NumRowsInserted} rows inserted, {pushResult.NumRowsDeleted} rows deleted, {pushResult.NumRowsUpdated} rows updated.");
            LogInformation($"PULL: {pullResult.NumRowsInserted} rows inserted, {pullResult.NumRowsDeleted} rows deleted, {pullResult.NumRowsUpdated} rows updated.");
            _dataState.SetCurrentSyncStatus(ESyncStatus.Synchronized);
            _dataState.SetLastSyncResult(_dataHelper.CombineSyncResults(pushResult, pullResult));

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
                pushResult = _dataHelper.CombineSyncResults(pushResult, await PushSyncableUserData(changedClientUser));
                _dataState.SetChangedUserData(null);
            }

            // SETTINGS DATA
            var changedClientSettings = _dataState.GetChangedSettingsData();
            if (changedClientSettings != null) // local changes have occurred
            {
                pushResult = _dataHelper.CombineSyncResults(pushResult, await PushSyncableUserData(changedClientSettings));
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
            var changedContexts = _dataState.GetChangedContextData();
            var sentExistingContexts = new HashSet<ContextDisplayModel>(ReferenceEqualityComparer.Instance);
            foreach (var clientContext in changedContexts.ToList())
            {
                bool isCreate = clientContext.Id == null;
                var clientLastUpdated = clientContext.ClientLastUpdated;
                DataSyncResult clientContextResult = await PushSyncableData(clientContext);
                // A create edited while its request was pending stays queued as an update for the next sync.
                if (!isCreate) sentExistingContexts.Add(clientContext);
                else if (clientContext.ClientLastUpdated == clientLastUpdated) changedContexts.Remove(clientContext);
                pushResult = _dataHelper.CombineSyncResults(pushResult, clientContextResult);
            }
            // Existing contexts leave the queue only once the whole push has succeeded.
            changedContexts.RemoveAll(sentExistingContexts.Contains);
            if (changedContexts.Count == 0) _dataState.SetTempContextId(0);

            // finalize, return result
            if (!_dataHelper.SyncChangesDetected(pushResult)) LogInformation("No changes detected on push.");
            LogInformation("Push complete.");
            return pushResult;
        }

        // process push at row level - user data
        private async Task<DataSyncResult> PushSyncableUserData(ISyncableUserData userData)
        {
            DataSyncResult result = new();
            ISyncableUserData serverData = userData.DataType == ESyncableUserDataType.User ?
                await _userEndpoint.GetCurrentUserData() : await _userEndpoint.GetCurrentUserSettings();

            // a change saved elsewhere after this one was made is kept (this sync's pull shows it here)
            if (QueuedChangeLoses(serverData, userData)) return result;

            // only the request is stamped, so a failed attempt leaves the queued change as it was
            DateTimeOffset savedAt = DateTimeOffset.Now;
            switch (userData)
            {
                case UserDisplayModel changedClientUser:

                    UserModel sentUser = _mapper.Map<UserModel>(changedClientUser);
                    sentUser.ServerLastUpdated = savedAt;
                    await _userEndpoint.UpdateName(sentUser);

                    UserDisplayModel savedUser = _dataState.GetCurrentUser()!.Clone();
                    (savedUser.FirstName, savedUser.LastName) = (changedClientUser.FirstName, changedClientUser.LastName);
                    (savedUser.ClientLastUpdated, savedUser.ServerLastUpdated) = (changedClientUser.ClientLastUpdated, savedAt);
                    ShowUser(savedUser);
                    _pushedUserData = changedClientUser;
                    result.NumRowsUpdated++;

                    break;

                case UserSettingsDisplayModel changedClientSettings:

                    UserSettingsModel sentSettings = _mapper.Map<UserSettingsModel>(changedClientSettings);
                    sentSettings.ServerLastUpdated = savedAt;
                    await _userEndpoint.UpdateUserSettings(sentSettings);

                    UserSettingsDisplayModel savedSettings = changedClientSettings.Clone();
                    savedSettings.ServerLastUpdated = savedAt;
                    ShowSettings(savedSettings);
                    _pushedUserSettingsData = changedClientSettings;
                    result.NumRowsUpdated++;

                    break;
            }

            return result;
        }

        // A queued name or settings change loses only to a change saved elsewhere since this device last loaded or saved
        // the row, and then only if that change was made later. The first test needs no clock, so a device whose clock
        // differs from the server's still saves an edit made against the row it holds.
        private static bool QueuedChangeLoses(ISyncableUserData serverRow, ISyncableUserData queued) =>
            serverRow.ServerLastUpdated != queued.ServerLastUpdated && serverRow.ClientLastUpdated > queued.ClientLastUpdated;

        // Puts these values (the server's row, or what was just saved or is waiting to be) in both local copies, with the
        // row's timestamps. The working copy, which the Settings pages edit, is replaced only when its values differ, so
        // a field being typed in isn't reset for nothing.
        private void ShowUser(UserDisplayModel user)
        {
            _dataState.SetCurrentUser(user);
            if (_dataState.GetWorkingCurrentUser() is not UserDisplayModel workingUser) return;
            if (_dataHelper.HasUserDataChanged(workingUser)) _dataState.SetWorkingCurrentUser(user.Clone());
            else (workingUser.ServerLastUpdated, workingUser.ClientLastUpdated) = (user.ServerLastUpdated, user.ClientLastUpdated);
        }

        // as ShowUser, for settings; cleanup reads these, so it follows the saved delays
        private void ShowSettings(UserSettingsDisplayModel settings)
        {
            _dataState.SetUserSettings(settings);
            if (_dataState.GetWorkingUserSettings() is not UserSettingsDisplayModel workingSettings) return;
            if (_dataHelper.HasSettingsDataChanged(workingSettings)) _dataState.SetWorkingUserSettings(settings.Clone());
            else (workingSettings.ServerLastUpdated, workingSettings.ClientLastUpdated) = (settings.ServerLastUpdated, settings.ClientLastUpdated);
        }

        // On signing in, a change whose save failed before signing out is still queued. Show it, so an edit made before
        // the next sync builds on it, unless the server already holds a later change, which the queued one would lose to.
        private void ShowQueuedUserData()
        {
            if (_dataState.GetChangedUserData() is UserDisplayModel queuedUser)
            {
                var loaded = _dataState.GetCurrentUser()!;
                if (QueuedChangeLoses(loaded, queuedUser)) _dataState.SetChangedUserData(null);
                else
                {
                    var shown = loaded.Clone();
                    (shown.FirstName, shown.LastName) = (queuedUser.FirstName, queuedUser.LastName);
                    ShowUser(shown);
                }
            }

            if (_dataState.GetChangedSettingsData() is UserSettingsDisplayModel queuedSettings)
            {
                var loaded = _dataState.GetUserSettings()!;
                if (QueuedChangeLoses(loaded, queuedSettings)) _dataState.SetChangedSettingsData(null);
                else
                {
                    var shown = loaded.Clone();
                    (shown.CleanUpImmediately, shown.CleanUpDelayDays, shown.DeleteDelayDays) =
                        (queuedSettings.CleanUpImmediately, queuedSettings.CleanUpDelayDays, queuedSettings.DeleteDelayDays);
                    ShowSettings(shown);
                }
            }
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

                        await PushNewContext((ContextDisplayModel)data);
                        break;
                }

                result.NumRowsInserted++;
            }
            else if (data.Deleted.HasValue) // delete
            {
                switch (data.DataType)
                {
                    case ESyncableDataType.Task:

                        // needs only the ID; the server accepts a task that is already gone
                        await _taskEndpoint.DeleteTask(_mapper.Map<TaskModel>((TaskDisplayModel)data));

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
                        
                result.NumRowsDeleted++;
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
                            if (index != -1)
                            {
                                data.Deleted = null; // deleted while sending, then restored by signing back in
                                _dataState.GetTasks()![index] = (TaskDisplayModel)data;
                            }

                            result. NumRowsUpdated++;
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

                            result.NumRowsUpdated++;
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

                            result.NumRowsUpdated++;
                        }
                        break;
                }
            }

            return result;
        }

        private async Task PushNewContext(ContextDisplayModel queued)
        {
            ContextModel acknowledged = await _contextEndpoint.AddContext(
                _mapper.Map<ContextModel>(queued), _dataState.GetCurrentUser()!.Id);
            var tempId = queued.TempLocalId;
            if (tempId.HasValue)
            {
                AcknowledgeNewContext(_dataState.GetContexts()!.Find(x => x.TempLocalId == tempId), acknowledged);
                AcknowledgeNewContext(_dataState.GetWorkingContexts()!.Find(x => x.TempLocalId == tempId), acknowledged);
            }
            AcknowledgeNewContext(queued, acknowledged);
            _pushedContextData.Add(acknowledged);
            if (acknowledged.Id is int createdId) _createdContextIds.Add(createdId);
        }

        private static void AcknowledgeNewContext(ContextDisplayModel? context, ContextModel acknowledged)
        {
            if (context == null) return;

            context.Id = acknowledged.Id;
            context.TempLocalId = null;
            context.UserId = acknowledged.UserId;
            context.ServerLastUpdated = acknowledged.ServerLastUpdated;
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
            // A retried create can carry a timestamp from before the current sign-in.
            var changedRemoteContextRows = serverContexts.Where(
                x => x.ServerLastUpdated >= _dataState.GetLastSync() ||
                     (x.Id is int id && _createdContextIds.Contains(id))).ToList();

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
            _createdContextIds.Clear();

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
                    // update local data store (with the row's own timestamps, which the next push compares)
                    switch (userData.DataType)
                    {
                        case ESyncableUserDataType.User:

                            UserModel serverUser = (UserModel)userData;
                            var displayServerUser = _mapper.Map<UserDisplayModel>(serverUser);
                            ShowUser(displayServerUser);

                            break;

                        case ESyncableUserDataType.Settings:

                            UserSettingsModel serverSettings = (UserSettingsModel)serverData;
                            var displayServerSettings = _mapper.Map<UserSettingsDisplayModel>(serverData); 
                            ShowSettings(displayServerSettings);

                            break;

                        default:
                            break;
                    }

                    result.NumRowsUpdated++;
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
                        result.NumRowsInserted++;
                    }
                    else // update
                    {
                        int i = _dataState.GetTasks()!.IndexOf(clientTask);
                        _dataState.GetTasks()![i] = displayServerTask;
                        result.NumRowsUpdated++;
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
                        result.NumRowsInserted++;
                    }
                    else // update
                    {
                        int i = _dataState.GetProjects()!.IndexOf(clientProject);
                        _dataState.GetProjects()![i] = displayServerProject;
                        result.NumRowsUpdated++;
                    }
                    break;

                case ESyncableDataType.Context:

                    if (_dataState.GetChangedContextData().Any(x => x.Id == data.Id && x.Deleted.HasValue)) return result;

                    ContextDisplayModel displayServerContext = (ContextDisplayModel)data;
                    ContextDisplayModel? clientContext = _dataState.GetContexts()!.Find(x => x.Id == data.Id);
                    ContextModel? pushedServerContext = _pushedContextData.Find(x => x.Id == data.Id);
                    bool restoreCreated = clientContext == null && data.Id is int id && _createdContextIds.Contains(id);
                    if (pushedServerContext != null && !restoreCreated) return result;

                    if (clientContext == null) // insert
                    {
                        if (pushedServerContext == null) result.NumRowsInserted++;
                        else displayServerContext.UserId ??= pushedServerContext.UserId;
                        _dataState.GetContexts()!.Add(displayServerContext.Clone());
                    }
                    else // update
                    {
                        int i = _dataState.GetContexts()!.IndexOf(clientContext);
                        _dataState.GetContexts()![i] = displayServerContext;
                        result.NumRowsUpdated++;
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
                            _dataState.GetTasks()!.Remove(clientDisplayTask);
                            pullDeletionsResult.NumRowsDeleted++;
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
                            pullDeletionsResult.NumRowsDeleted++;
                        }
                    }

                    break;

                case ESyncableDataType.Context:

                    List<ContextModel> serverContexts = await _contextEndpoint.GetAllContextsForUser();

                    foreach (var clientDisplayContext in _dataState.GetContexts()!.ToList())
                    {
                        ContextModel? serverContext = serverContexts.Find(x => x.Id == clientDisplayContext.Id);
                        if (clientDisplayContext.Id.HasValue && serverContext == null) // not found on server? delete locally
                        {
                            // ensure other contexts have updated OrderIndex values
                            _dataService.ShiftCollectionOrderIndices(clientDisplayContext, _dataState.GetContexts()!);
                            _dataState.GetContexts()!.Remove(clientDisplayContext);
                            pullDeletionsResult.NumRowsDeleted++;
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
