using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Diagnostics;
using System.IO.Pipelines;
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
    public class DataSyncService : IDataSyncService
    {
        private ILogger<DataSyncService> _logger;
        private IMapper _mapper;
        private IDataHelper _dataHelper;
        private IDataState _dataState;
        private IDataService _dataService;

        private IUserEndpoint _userEndpoint;
        private ITaskEndpoint _taskEndpoint;
        private IProjectEndpoint _projectEndpoint;
        private IContextEndpoint _contextEndpoint;

        private UserDisplayModel? _pushedUserData;
        private UserSettingsDisplayModel? _pushedUserSettingsData;
        private List<TaskModel> _pushedTaskData;
        private List<ProjectModel> _pushedProjectData;
        private List<ContextModel> _pushedContextData;

        private bool _syncInProgress = false;

        public DataSyncService(ILogger<DataSyncService> logger,
                               IMapper mapper,
                               IDataHelper dataHelper,
                               IDataState dataState,
                               IDataService dataService,
                               IUserEndpoint userEndpoint, 
                               ITaskEndpoint taskEndpoint, 
                               IProjectEndpoint projectEndpoint,
                               IContextEndpoint contextEndpoint)
        {
            _logger = logger;
            _mapper = mapper;
            _dataHelper = dataHelper;
            _dataState = dataState;
            _dataService = dataService;

            _userEndpoint = userEndpoint;
            _taskEndpoint = taskEndpoint;
            _projectEndpoint = projectEndpoint;
            _contextEndpoint = contextEndpoint;

            _pushedUserData = new();
            _pushedUserSettingsData = new();
            _pushedTaskData = new();
            _pushedProjectData = new();
            _pushedContextData = new();

            _dataService.SyncRequestHandler += DataService_SyncRequested;
            _dataService.PreLogoutSyncRequestHandler += DataService_PreLogoutSyncRequested;
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

        private async void DataService_SyncRequested(object? sender, string e)
        {
            LogInformation($"{e}'s requested sync operation received.");
            bool success = await TrySync();
            if (success) LogInformation($"{e}'s requested sync operation has been handled.");
            else { LogError($"{e}'s requested sync operation failed."); }
        }

        private async void DataService_PreLogoutSyncRequested(object? sender, string e)
        {
            bool success = await TrySync(true);
            if (success) LogInformation($"{e}'s requested pre-logout sync operation has been handled.");
            else { LogError($"{e}'s requested pre-logout sync operation failed."); }
        }

        // attempts to Sync(), and re-attempts if an exception is raised
        public async Task<bool> TrySync(bool isPreLogoutSync = false)
        {
            var numRetryAttempts = 3;
            var retryDelay = 1000;
            bool isInitialSync = _dataState.LastSync == DateTimeOffset.MinValue;
            //Console.WriteLine($"TrySync() - isInitialSync: {isInitialSync}");

            for (int i = 0; i < numRetryAttempts; i++)
            {
                try
                {
                    // attempt sync; if successful, break loop
                    if (isInitialSync) await InitSync();
                    else await Sync(isPreLogoutSync);

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
                catch (Exception ex)
                {
                    LogError(ex.Message);
                }
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
                _dataState.LastSync = DateTimeOffset.Now; // log
                LogInformation("InitSync complete; DataState populated");

                // initialize periodic sync
                TimeSpan interval = TimeSpan.FromSeconds(60);
                //await PeriodicSync(interval); TODO: temporarily disabled during testing
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
        private async Task Sync(bool isPreLogoutSync = false)
        {
            if (_syncInProgress) throw new Exception("Sync already in progress; operation aborted.");

            _syncInProgress = true;
            LogInformation($"starting Sync... (lastSync prior: {_dataState.LastSync}");
            LogInformation($"Sync() - isPreLogoutSync: {isPreLogoutSync}");

            // process any time-relevant changes to task state
            _dataService.PerformCompletedTaskCleanup();

            // push all client rows locally changed since last sync to the server
            DataSyncResult pushResult = await PushSync();
            // pull all records that have changed since last sync...
            // and update the local client data with any changes (inserts, deletions, updates)
            DataSyncResult pullResult = await PullSync();

            // update "working" copies of local data from newly synced data state + log completion
            _dataService.UpdateAllWorkingDataAfterPull();
            _dataState.LastSync = DateTimeOffset.Now;

            LogInformation("Sync complete!");
            LogInformation($"PUSH: {pushResult.numRowsInserted} rows inserted, {pushResult.numRowsDeleted} rows deleted, {pushResult.numRowsUpdated} rows updated.");
            LogInformation($"PULL: {pullResult.numRowsInserted} rows inserted, {pullResult.numRowsDeleted} rows deleted, {pullResult.numRowsUpdated} rows updated.");
            _syncInProgress = false;

            // trigger logout in client app, if requested
            if (isPreLogoutSync) _dataState.PreLogoutSyncCompleted = true;          
        }

        private async Task<DataSyncResult> PushSync()
        {
            LogInformation("Pushing changes to server...");
            DataSyncResult pushResult = new();

            // here, we either insert or update any server data that has been changed locally since last sync
            // we don't do any other update-triggered data processing (shifting indices, updating other data dependent on this, etc)
            // as that's all handled locally by the DataService, on the DataState, when changes occur.
            // DataSyncService is solely dedicated to trusting the local datastate and making sure it's in sync with the server database.

            // FOR TASKS/PROJECTS/CONTEXTS:
            // 1. check if any changed local data rows don't exist in remote
            // 2. if so, insert
            // 3. else, update remote row with local one

            // FOR USER DATA / SETTINGS DATA:
            // simply update (using either server or client data), no insert/delete possible

            // USER DATA (name only; email/pw are handled separately from syncable data)
            //var clientUser = _dataState.ChangedUserData.FirstOrDefault(); // will only ever be one (or zero)
            var changedClientUser = _dataState.ChangedUserData;
            if (changedClientUser != null) // local changes have occurred
            {
                LogInformation("changed UserData detected");
                //LogInformation($"_dataState.ChangedUserData.Email: {_dataState.ChangedUserData.Email}");
                await PushSyncableUserData(changedClientUser);
                _dataState.ChangedUserData = null;

                //UserModel serverUser = await _userEndpoint.GetCurrentUserData();

                //if (serverUser.ClientLastUpdated > clientUser!.ServerLastUpdated)
                //{
                //    // conflict - server data is newer than client
                //    // server wins; ignore changes and just update time
                //    serverUser.ServerLastUpdated = DateTimeOffset.Now;
                //    serverUser.ClientLastUpdated = clientUser.ServerLastUpdated;
                //    await _userEndpoint.UpdateName(serverUser);
                //}
                //else // client data is newer than server
                //{
                //    clientUser.ServerLastUpdated = DateTimeOffset.Now;
                //    await _userEndpoint.UpdateName(_mapper.Map<UserModel>(clientUser));
                //    //_pushedUserData.Add(clientUser);
                //    _pushedUserData = clientUser;
                //    pushResult.numRowsUpdated++;
                //}

                //_dataState.ChangedUserData.Clear();
                //_dataState.ChangedUserData = null;
            }

            // SETTINGS DATA
            var changedClientSettings = _dataState.ChangedUserSettingsData;
            if (changedClientSettings != null) // local changes have occurred
            {
                LogInformation("changed UserSettingsData detected");
                //LogInformation($"_dataState.ChangedUserSettingsData.CleanUpImmediately: {_dataState.ChangedUserSettingsData.CleanUpImmediately}");
                await PushSyncableUserData(changedClientSettings);
                _dataState.ChangedUserSettingsData = null;

                //UserSettingsModel serverSettings = await _userEndpoint.GetCurrentUserSettings();
                //if (serverSettings.ClientLastUpdated > clientSettings!.ServerLastUpdated)
                //{
                //    // conflict - server data is newer than client
                //    // server wins; ignore changes and just update time
                //    serverSettings.ServerLastUpdated = DateTimeOffset.Now;
                //    serverSettings.ClientLastUpdated = clientSettings.ServerLastUpdated;
                //    await _userEndpoint.UpdateUserSettings(serverSettings);
                //}
                //else // client data is newer than server
                //{
                //    clientSettings.ServerLastUpdated = DateTimeOffset.Now;
                //    await _userEndpoint.UpdateUserSettings(_mapper.Map<UserSettingsModel>(clientSettings));
                //    _pushedUserSettingsData = clientSettings;
                //    pushResult.numRowsUpdated++;
                //}

                //_dataState.ChangedUserSettingsData = null;        
            }

            // TASK DATA
            Console.WriteLine($"changedTaskData count: {_dataState.ChangedTaskData.Count}");
            foreach (var clientTask in _dataState.ChangedTaskData)
            {
                DataSyncResult clientTaskResult = await PushSyncableData(clientTask);
                pushResult = _dataHelper.CombineSyncResults(pushResult, clientTaskResult);
            }
            _dataState.ChangedTaskData.Clear();
            _dataState.TempTaskId = 0;

            // PROJECT DATA
            Console.WriteLine($"changedProjectData count: {_dataState.ChangedProjectData.Count}");
            foreach (var clientProject in _dataState.ChangedProjectData)
            {
                DataSyncResult clientProjectResult = await PushSyncableData(clientProject);
                pushResult = _dataHelper.CombineSyncResults(pushResult, clientProjectResult);
            }
            _dataState.ChangedProjectData.Clear();
            _dataState.TempProjectId = 0;

            // CONTEXT DATA
            Console.WriteLine($"changedContextData count: {_dataState.ChangedContextData.Count}");
            foreach (var clientContext in _dataState.ChangedContextData)
            {
                DataSyncResult clientContextResult = await PushSyncableData(clientContext);
                pushResult = _dataHelper.CombineSyncResults(pushResult, clientContextResult);
            }
            _dataState.ChangedContextData.Clear();
            _dataState.TempContextId = 0;

            // finalize, return result

            if (!_dataHelper.SyncChangesDetected(pushResult))
            {

                LogInformation("no changes detected on push.");
            }

            LogInformation("Push complete.");
            //Console.WriteLine($"dataState.Tasks count: {_dataState.Tasks.Count}");
            return pushResult;
        }

        private async Task<DataSyncResult> PushSyncableUserData(ISyncableUserData userData)
        {
            LogInformation($"PushSyncableUserData() start");
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
                    if (serverWon)
                    {
                        await _userEndpoint.UpdateName(_mapper.Map<UserModel>(serverUser));
                    }
                    else
                    {
                        _pushedUserData = changedClientUser;
                        result.numRowsUpdated++;
                    }

                    break;

                case ESyncableUserDataType.Settings:

                    UserSettingsDisplayModel changedClientSettings = (UserSettingsDisplayModel)changedClientData;
                    UserSettingsDisplayModel serverSettings = (UserSettingsDisplayModel)serverData;
                    if (serverWon)
                    {                        
                        await _userEndpoint.UpdateUserSettings(_mapper.Map<UserSettingsModel>(serverSettings));
                    }
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

        private async Task<DataSyncResult> PushSyncableData(ISyncableData data)
        {
            Console.WriteLine($"PushSyncableData() start");
           // Console.WriteLine($"dataState.Tasks count: {_dataState.Tasks.Count}");

            DataSyncResult result = new();

            if (data.Id == null && data.Deleted.HasValue)
            {
                // was deleted locally before pushed to server; ignore
                return result;
            }

            Trace.WriteLine("changes detected on push!");
            Console.WriteLine("changes detected on push!");

            if (data.Id == null) // doesn't exist on server; insert
            {
                if (data.ServerLastUpdated == DateTimeOffset.MinValue)
                    data.ServerLastUpdated = DateTimeOffset.Now;

                switch (data.DataType)
                {
                    case ESyncableDataType.Task:

                        TaskModel insertedTask = await _taskEndpoint.AddTask(_mapper.Map<TaskModel>((TaskDisplayModel)data), _dataState.GetCurrentUser()!.Id);
                        Console.WriteLine($"inserted task with new server-made id: {insertedTask.Id}");

                        _pushedTaskData.Add(_mapper.Map<TaskModel>((TaskDisplayModel)data));

                        int taskIndex = _dataState.GetTasks()!.FindIndex(x => x.TempLocalId == data.TempLocalId);
                        _dataState.GetTasks()![taskIndex].Id = insertedTask.Id; // update with new server-granted id
                        _dataState.GetTasks()![taskIndex].TempLocalId = null; //

                        //if (index != -1) { _dataState.Tasks[index] = (TaskDisplayModel)data; }
                        //Console.WriteLine($"removing local pre-push version of {_dataState.Tasks![index].TaskName}");
                        //_dataState.Tasks!.RemoveAt(index); // remove local version, to be replaced shortly by pulled one with an Id given by server

                        break;

                    case ESyncableDataType.Project:

                        ProjectModel insertedProject = await _projectEndpoint.AddProject(_mapper.Map<ProjectModel>((ProjectDisplayModel)data), _dataState.GetCurrentUser()!.Id);
                        Console.WriteLine($"inserted project with new server-made id: {insertedProject.Id}");

                        _pushedProjectData.Add(_mapper.Map<ProjectModel>((ProjectDisplayModel)data));

                        int projectIndex = _dataState.GetProjects()!.FindIndex(x => x.TempLocalId == data.TempLocalId);
                        _dataState.GetProjects()![projectIndex].Id = insertedProject.Id; // update with new server-granted id
                        _dataState.GetProjects()![projectIndex].TempLocalId = null; //

                        break;

                    case ESyncableDataType.Context:

                        ContextModel insertedContext= await _contextEndpoint.AddContext(_mapper.Map<ContextModel>((ContextDisplayModel)data), _dataState.GetCurrentUser()!.Id);
                        Console.WriteLine($"inserted context with new server-made id: {insertedContext.Id}");

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

                            Console.WriteLine("Push conflict detected! Local changes ignored; only time updated");
                            Trace.WriteLine("Push conflict detected! Local changes ignored; only time updated");
                        }
                        else // client data is newer than server
                        {
                            Console.WriteLine($"clientTask.ServerLastUpdated: {data.ServerLastUpdated}, clientTask.ClientLastUpdated: {data.ClientLastUpdated}");
                            Trace.WriteLine($"clientTask.ServerLastUpdated: {data.ServerLastUpdated}, clientTask.ClientLastUpdated: {data.ClientLastUpdated}");

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

                            Console.WriteLine("Push conflict detected! Local changes ignored; only time updated");
                            Trace.WriteLine("Push conflict detected! Local changes ignored; only time updated");
                        }
                        else // client data is newer than server
                        {
                            Console.WriteLine($"clientProject.ServerLastUpdated: {data.ServerLastUpdated}, clientProject.ClientLastUpdated: {data.ClientLastUpdated}");
                            Trace.WriteLine($"clientProject.ServerLastUpdated: {data.ServerLastUpdated}, clientProject.ClientLastUpdated: {data.ClientLastUpdated}");

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

                            LogInformation("Push conflict detected! Local changes ignored; only time updated");
                        }
                        else // client data is newer than server
                        {
                            LogInformation($"clientContext.ServerLastUpdated: {data.ServerLastUpdated}, clientContext.ClientLastUpdated: {data.ClientLastUpdated}");

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

        // gets all records that have changed since LastSync
        // updates local client data with any changes (inserts, deletions, updates)
        private async Task<DataSyncResult> PullSync()
        {
            LogInformation("Pulling changes from server...");
            LogInformation($"PullSync() start");
            //Console.WriteLine($"dataState.Tasks count: {_dataState.Tasks.Count}");
            DataSyncResult pullResult = new();

            // USER DATA
            UserModel serverUser = await _userEndpoint.GetCurrentUserData();
            if (serverUser.ServerLastUpdated >= _dataState.LastSync) // remote changes have occurred
            {
                LogInformation("changed UserData detected");
                await PullSyncableUserData(serverUser);
            }
            _pushedUserData = null; // no longer needed for reference

            //UserModel serverCurrentUser = await _userEndpoint.GetCurrentUserData();
            //IList<UserModel> serverUserRows = [serverCurrentUser];
            //var changedRemoteUserRows = serverUserRows.Where(
            //    x => x.ServerLastUpdated >= _dataState.LastSync).ToList();

            //foreach (var serverUser in changedRemoteUserRows)
            //{
            //    // do not pull if we just pushed the change
            //    //var pushedServerUser = _pushedUserData.Where(x => x.Id == serverUser.Id);
            //    var pushedServerUser = _pushedUserData;
            //    //if (pushedServerUser.Count() != 0) { continue; }
            //    if (pushedServerUser != null) { continue; }

            //    //Trace.WriteLine("serverCurrentUser - changes detected on pull!");
            //    //Trace.WriteLine($"serverUser.ServerLastUpdated: {serverUser.ServerLastUpdated}; LastSync: {_dataState.LastSync}; a >= b: {serverUser.ServerLastUpdated >= _dataState.LastSync}");
            //    //Console.WriteLine("serverCurrentUser - changes detected on pull!");
            //    //Console.WriteLine($"serverUser.ServerLastUpdated: {serverUser.ServerLastUpdated}; LastSync: {_dataState.LastSync}; a >= b: {serverUser.ServerLastUpdated >= _dataState.LastSync}");

            //    UserDisplayModel displayServerUser = _mapper.Map<UserDisplayModel>(serverUser);
            //    displayServerUser.ServerLastUpdated = DateTimeOffset.Now;
            //    // update local data store
            //    _dataState.CurrentUser = displayServerUser;
            //    pullResult.numRowsUpdated++;
            //}

            //if (changedRemoteUserRows.Count == 0)
            //{
            //    Trace.WriteLine("serverCurrentUser - no changes detected on pull.");
            //    Trace.WriteLine($"serverUser.ServerLastUpdated: {serverCurrentUser.ServerLastUpdated}; LastSync: {_dataState.LastSync}; a >= b: {serverCurrentUser.ServerLastUpdated >= _dataState.LastSync}");
            //    Console.WriteLine("serverCurrentUser - no changes detected on pull.");
            //    Console.WriteLine($"serverUser.ServerLastUpdated: {serverCurrentUser.ServerLastUpdated}; LastSync: {_dataState.LastSync}; a >= b: {serverCurrentUser.ServerLastUpdated >= _dataState.LastSync}");
            //}

            //_pushedUserData.Clear();
            //_pushedUserData = null; // no longer needed for reference

            // SETTINGS DATA
            UserSettingsModel serverSettings = await _userEndpoint.GetCurrentUserSettings();
            if (serverSettings.ServerLastUpdated >= _dataState.LastSync) // remote changes have occurred
            {
                LogInformation("changed UserSettingsData detected");
                await PullSyncableUserData(serverSettings);
                
                //// only pull if we didn't just push the change
                //if (_pushedUserSettingsData == null)
                //{
                //    LogInformation($"PullSync found changedRemoteUserSettings");

                //    UserSettingsDisplayModel displayServerSettings = _mapper.Map<UserSettingsDisplayModel>(serverSettings);
                //    displayServerSettings.ServerLastUpdated = DateTimeOffset.Now;
                //    // update local data store
                //    _dataState.SetUserSettings(displayServerSettings);
                //    pullResult.numRowsUpdated++;
                //}           
            }
            _pushedUserSettingsData = null; // no longer needed for reference


            // TASK DATA
            DataSyncResult tasksPullResult = new();
            List<TaskModel> serverTasks = await _taskEndpoint.GetAllTasksForUser();
            var changedRemoteTaskRows = serverTasks.Where(
                x => x.ServerLastUpdated >= _dataState.LastSync).ToList();

            LogInformation($"changedRemoteTaskRows count: {changedRemoteTaskRows.Count}");

            // handle local inserts/updates (originating from another client)
            foreach (var serverTask in changedRemoteTaskRows)
            {
                LogInformation($"PullSync found changedRemoteTaskRow: {serverTask.TaskName}");
                DataSyncResult serverTaskResult = PullSyncableData(_mapper.Map<TaskDisplayModel>(serverTask));
                tasksPullResult = _dataHelper.CombineSyncResults(tasksPullResult, serverTaskResult);
            }

            // handle local deletions (originating from another client)
            DataSyncResult taskDeletionResult = await PullServerDataDeletions(ESyncableDataType.Task);
            tasksPullResult = _dataHelper.CombineSyncResults(tasksPullResult, taskDeletionResult); 

            if (changedRemoteTaskRows.Count == 0 && tasksPullResult.numRowsDeleted == 0)
            {
                LogInformation("serverTask - no changes detected on pull.");
            }

            // store copy for comparison + cleanup temp push history
            //_dataHelper.TasksLastFetch = serverTasks;
            //_dataState.WorkingTasks = serverTasks;
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
                x => x.ServerLastUpdated >= _dataState.LastSync).ToList();

            LogInformation($"changedRemoteProjectRows count: {changedRemoteProjectRows.Count}");

            // handle local inserts/updates (originating from another client)
            foreach (var serverProject in changedRemoteProjectRows)
            {
                LogInformation($"PullSync found changedRemoteProjectRow: {serverProject.ProjectName}");
                DataSyncResult serverProjectResult = PullSyncableData(_mapper.Map<ProjectDisplayModel>(serverProject));
                projectsPullResult = _dataHelper.CombineSyncResults(projectsPullResult, serverProjectResult);
            }

            // handle local deletions (originating from another client)
            DataSyncResult projectDeletionResult = await PullServerDataDeletions(ESyncableDataType.Project);
            projectsPullResult = _dataHelper.CombineSyncResults(projectsPullResult, projectDeletionResult);

            if (changedRemoteProjectRows.Count == 0 && projectsPullResult.numRowsDeleted == 0)
            {
                LogInformation("Projects - no changes detected on pull.");
            }

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
                x => x.ServerLastUpdated >= _dataState.LastSync).ToList();

            LogInformation($"changedRemoteContextRows count: {changedRemoteContextRows.Count}");

            // handle local inserts/updates (originating from another client)
            foreach (var serverContext in changedRemoteContextRows)
            {
                LogInformation($"PullSync found changedRemoteContextRows: {serverContext.ContextName}");
                DataSyncResult serverContextResult = PullSyncableData(_mapper.Map<ContextDisplayModel>(serverContext));
                contextsPullResult = _dataHelper.CombineSyncResults(contextsPullResult, serverContextResult);
            }

            // handle local deletions (originating from another client)
            DataSyncResult contextDeletionResult = await PullServerDataDeletions(ESyncableDataType.Context);
            contextsPullResult = _dataHelper.CombineSyncResults(contextsPullResult, contextDeletionResult);

            if (changedRemoteContextRows.Count == 0 && contextsPullResult.numRowsDeleted == 0)
            {
                LogInformation("Contexts - no changes detected on pull.");
            }

            // cleanup temp push history
            _pushedContextData.Clear();

            // trigger UI update if needed + add to total pull result
            if (_dataHelper.SyncChangesDetected(contextsPullResult))
            {
                _dataState.InvokeDataStateChanged(nameof(EDataRefreshType.Contexts));
                pullResult = _dataHelper.CombineSyncResults(pullResult, contextsPullResult);
            }

            // finalize, return result
            if (!_dataHelper.SyncChangesDetected(pullResult))
            {

                LogInformation("no changes detected on pull.");
            }

            LogInformation("Pull complete.");
            return pullResult;
        }

        private async Task<DataSyncResult> PullSyncableUserData(ISyncableUserData userData)
        {
            LogInformation($"PullSyncableUserData() start");
            DataSyncResult result = new();

            ISyncableUserData serverData = userData.DataType == ESyncableUserDataType.User ?
                await _userEndpoint.GetCurrentUserData() : await _userEndpoint.GetCurrentUserSettings();

            if (serverData.ServerLastUpdated >= _dataState.LastSync)
            {
                // only pull if we didn't just push the change
                if (userData.DataType == ESyncableUserDataType.User && _pushedUserData == null ||
                    userData.DataType == ESyncableUserDataType.Settings && _pushedUserSettingsData == null)
                {
                    LogInformation("PullSync found ISyncableUserData to pull");

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

        // handles local insert/deletes based on remote data row
        private DataSyncResult PullSyncableData(ISyncableData data)
        {
            Console.WriteLine($"PullSyncableData() start");
            //Console.WriteLine($"dataState.Tasks count: {_dataState.Tasks.Count}");
            DataSyncResult result = new();

            switch (data.DataType)
            {
                case ESyncableDataType.Task:

                    // do not pull if we just pushed the change
                    var pushedServerTask = _pushedTaskData.Where(x => x.Id == data.Id);
                    if (pushedServerTask.Any()) { return result; }

                    TaskDisplayModel displayServerTask = (TaskDisplayModel)data;
                    TaskDisplayModel? clientTask = _dataState.GetTasks()!.Where(
                        x => x.Id == displayServerTask.Id).FirstOrDefault();

                    if (clientTask == null) // insert
                    {
                        _dataState.GetTasks()!.Add(displayServerTask.Clone());
                        Console.WriteLine($"added local task on PullSyncableData: {displayServerTask.TaskName}");
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
                    if (pushedServerProject.Any()) { return result; }

                    ProjectDisplayModel displayServerProject = (ProjectDisplayModel)data;
                    ProjectDisplayModel? clientProject = _dataState.GetProjects()!.Where(
                        x => x.Id == displayServerProject.Id).FirstOrDefault();

                    if (clientProject == null) // insert
                    {
                        _dataState.GetProjects()!.Add(displayServerProject.Clone());
                        LogInformation($"added local project on PullSyncableData: {displayServerProject.ProjectName}");
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
                    if (pushedServerContext.Any()) { return result; }

                    ContextDisplayModel displayServerContext = (ContextDisplayModel)data;
                    ContextDisplayModel? clientContext = _dataState.GetContexts()!.Where(
                        x => x.Id == displayServerContext.Id).FirstOrDefault();

                    if (clientContext == null) // insert
                    {
                        _dataState.GetContexts()!.Add(displayServerContext.Clone());
                        LogInformation($"added local context on PullSyncableData: {displayServerContext.ContextName}");
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
            Console.WriteLine($"PullServerDataDeletions() start");
            //Console.WriteLine($"dataState.Tasks count: {_dataState.Tasks.Count}");
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
                            Console.WriteLine($"PullServerDataDeletions found local row deleted on server: {clientDisplayTask.TaskName}");
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
                            LogInformation($"PullServerDataDeletions found local row deleted on server: {clientDisplayProject.ProjectName}");
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
                            LogInformation($"PullServerDataDeletions found local row deleted on server: {clientDisplayContext.ContextName}");
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
