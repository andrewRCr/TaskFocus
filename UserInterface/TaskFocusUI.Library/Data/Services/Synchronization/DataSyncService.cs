using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Diagnostics;
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

        private List<UserModel> _pushedUserData;
        private List<UserSettingsModel> _pushedUserSettingsData;
        private List<TaskModel> _pushedTaskData;
        private List<ProjectModel> _pushedProjectData;
        private List<ContextModel> _pushedContextData;

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
        }

        // populate empty DataState + initialize on client launch
        public async Task InitSync()
        {
            await _dataService.FetchAllRemoteData();   
            _dataState.LastSync = DateTimeOffset.Now; // log
            //Console.WriteLine($"InitSync complete; DataState populated.");
            //Trace.WriteLine($"InitSync complete; DataState populated.");
            _logger.LogInformation("InitSync complete; DataState populated");

            // periodic sync
            TimeSpan interval = TimeSpan.FromSeconds(60);
            await PeriodicSync(interval);
        }

        public async Task PeriodicSync(TimeSpan interval, CancellationToken cancellationToken = default)
        {
            using PeriodicTimer timer = new(interval);
            while (true)
            {
                try
                {
                    await timer.WaitForNextTickAsync(cancellationToken);
                    await Sync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
        }

        // syncs at row level; LastUpdated determines who wins at the server
        public async Task Sync()
        {
            // debug
            Console.WriteLine($"starting Sync... (lastSync prior: {_dataState.LastSync}");
            Trace.WriteLine($"starting Sync... (lastSync prior: {_dataState.LastSync}");

            // push all client rows locally changed since last sync to the server
            //int[] numPushedRows = await PushSync();
            DataSyncResult pushResult = await PushSync();
            // pull all records that have changed since last sync...
            // and update the local client data with any changes (inserts, deletions, updates)
            DataSyncResult pullResult = await PullSync();

            _dataState.LastSync = DateTimeOffset.Now;

            Console.WriteLine($"Sync complete!");
            Console.WriteLine($"PUSH: {pushResult.numRowsInserted} rows inserted, {pushResult.numRowsDeleted} rows deleted, {pushResult.numRowsUpdated} rows updated.");
            Console.WriteLine($"PULL: {pullResult.numRowsInserted} rows inserted, {pullResult.numRowsDeleted} rows deleted, {pullResult.numRowsUpdated} rows updated.");
            Trace.WriteLine($"Sync complete!");
            Trace.WriteLine($"PUSH: {pushResult.numRowsInserted} rows inserted, {pushResult.numRowsDeleted} rows deleted, {pushResult.numRowsUpdated} rows updated.");
            Trace.WriteLine($"PULL: {pullResult.numRowsInserted} rows inserted, {pullResult.numRowsDeleted} rows deleted, {pullResult.numRowsUpdated} rows updated.");


        }

        private async Task<DataSyncResult> PushSync()
        {
            Console.WriteLine("Pushing changes to server...");
            Trace.WriteLine("Pushing changes to server...");
            Console.WriteLine($"dataState.Tasks count: {_dataState.GetTasks()!.Count}");
            //int numRowsInserted = 0;
            //int numRowsDeleted = 0;
            //int numRowsUpdated = 0;
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
            var clientUser = _dataState.ChangedUserData.FirstOrDefault(); // will only ever be one (or zero)
            if (clientUser != null) // local changes have occured
            {
                UserModel serverUser = await _userEndpoint.GetCurrentUserData();

                if (serverUser.ClientLastUpdated > clientUser!.ServerLastUpdated)
                {
                    // conflict - server data is newer than client
                    // server wins; ignore changes and just update time
                    serverUser.ServerLastUpdated = DateTimeOffset.Now;
                    serverUser.ClientLastUpdated = clientUser.ServerLastUpdated;
                    await _userEndpoint.UpdateName(serverUser);
                }
                else // client data is newer than server
                {
                    clientUser.ServerLastUpdated = DateTimeOffset.Now;
                    await _userEndpoint.UpdateName(clientUser);
                    _pushedUserData.Add(clientUser);
                    pushResult.numRowsUpdated++;
                }

                _dataState.ChangedUserData.Clear();
            }

            // SETTINGS DATA
            //foreach (var row in _dataState.ChangedUserSettingsData)
            //    await _userEndpoint.UpdateUserSettings(row);
            //_dataState.ChangedUserSettingsData.Clear();

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
            //foreach (var row in _dataState.ChangedContextData)
            //    await _contextEndpoint.UpdateContext(row);
            //_dataState.ChangedContextData.Clear();


            if (!_dataHelper.SyncChangesDetected(pushResult))
            {
                Trace.WriteLine("no changes detected on push.");
                Console.WriteLine("no changes detected on push.");
            }

            Trace.WriteLine("Push complete.");
            Console.WriteLine("Push complete.");
            //Console.WriteLine($"dataState.Tasks count: {_dataState.Tasks.Count}");
            return pushResult;
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

                        TaskModel insertedTask = await _taskEndpoint.AddTask(_mapper.Map<TaskModel>((TaskDisplayModel)data), _dataState.CurrentUser!.Id);
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

                        ProjectModel insertedProject = await _projectEndpoint.AddProject(_mapper.Map<ProjectModel>((ProjectDisplayModel)data), _dataState.CurrentUser!.Id);
                        Console.WriteLine($"inserted project with new server-made id: {insertedProject.Id}");

                        _pushedProjectData.Add(_mapper.Map<ProjectModel>((ProjectDisplayModel)data));

                        int projectIndex = _dataState.GetProjects()!.FindIndex(x => x.TempLocalId == data.TempLocalId);
                        _dataState.GetProjects()![projectIndex].Id = insertedProject.Id; // update with new server-granted id
                        _dataState.GetProjects()![projectIndex].TempLocalId = null; //

                        break;

                    case ESyncableDataType.Context:
                        break;
                    case ESyncableDataType.Settings:
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
                        break;
                    case ESyncableDataType.Settings:
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
                            Console.WriteLine($"clientTask.ServerLastUpdated: {data.ServerLastUpdated}, clientTask.ClientLastUpdated: {data.ClientLastUpdated}");
                            Trace.WriteLine($"clientTask.ServerLastUpdated: {data.ServerLastUpdated}, clientTask.ClientLastUpdated: {data.ClientLastUpdated}");

                            data.ServerLastUpdated = DateTimeOffset.Now;
                            await _projectEndpoint.UpdateProject(_mapper.Map<ProjectModel>(data));

                            _pushedProjectData.Add(_mapper.Map<ProjectModel>((ProjectDisplayModel)data));
                            int index = _dataState.GetProjects()!.FindIndex(x => x.Id == data.Id);
                            if (index != -1) { _dataState.GetProjects()![index] = (ProjectDisplayModel)data; }

                            result.numRowsUpdated++;
                        }
                        break;

                    case ESyncableDataType.Context:
                        break;
                    case ESyncableDataType.Settings:
                        break;
                }
            }

            return result;
        }


        // gets all records that have changed since LastSync
        // updates local client data with any changes (inserts, deletions, updates)
        private async Task<DataSyncResult> PullSync()
        {
            Console.WriteLine("Pulling changes from server...");
            Console.WriteLine($"PullSync() start");
            //Console.WriteLine($"dataState.Tasks count: {_dataState.Tasks.Count}");
            //Console.WriteLine($"dataState.Tasks count: {_dataState.Tasks.Count}");
            DataSyncResult pullResult = new();

            // USER DATA
            UserModel serverCurrentUser = await _userEndpoint.GetCurrentUserData();
            IList<UserModel> serverUserRows = [serverCurrentUser];
            var changedRemoteUserRows = serverUserRows.Where(
                x => x.ServerLastUpdated >= _dataState.LastSync).ToList();

            foreach (var serverUser in changedRemoteUserRows)
            {
                // do not pull if we just pushed the change
                var pushedServerUser = _pushedUserData.Where(x => x.Id == serverUser.Id);
                if (pushedServerUser.Count() != 0) { continue; }

                //Trace.WriteLine("serverCurrentUser - changes detected on pull!");
                //Trace.WriteLine($"serverUser.ServerLastUpdated: {serverUser.ServerLastUpdated}; LastSync: {_dataState.LastSync}; a >= b: {serverUser.ServerLastUpdated >= _dataState.LastSync}");
                //Console.WriteLine("serverCurrentUser - changes detected on pull!");
                //Console.WriteLine($"serverUser.ServerLastUpdated: {serverUser.ServerLastUpdated}; LastSync: {_dataState.LastSync}; a >= b: {serverUser.ServerLastUpdated >= _dataState.LastSync}");

                UserDisplayModel displayServerUser = _mapper.Map<UserDisplayModel>(serverUser);
                displayServerUser.ServerLastUpdated = DateTimeOffset.Now;
                // update local data store + store copy for comparison
                _dataState.CurrentUser = displayServerUser;
                //_dataHelper.UserDataLastFetch = _dataState.CurrentUser;
                pullResult.numRowsUpdated++;
            }

            //if (changedRemoteUserRows.Count == 0)
            //{
            //    Trace.WriteLine("serverCurrentUser - no changes detected on pull.");
            //    Trace.WriteLine($"serverUser.ServerLastUpdated: {serverCurrentUser.ServerLastUpdated}; LastSync: {_dataState.LastSync}; a >= b: {serverCurrentUser.ServerLastUpdated >= _dataState.LastSync}");
            //    Console.WriteLine("serverCurrentUser - no changes detected on pull.");
            //    Console.WriteLine($"serverUser.ServerLastUpdated: {serverCurrentUser.ServerLastUpdated}; LastSync: {_dataState.LastSync}; a >= b: {serverCurrentUser.ServerLastUpdated >= _dataState.LastSync}");
            //}

            _pushedUserData.Clear();

            // SETTINGS DATA

            // TASK DATA
            DataSyncResult tasksPullResult = new();
            List<TaskModel> serverTasks = await _taskEndpoint.GetAllTasksForUser();
            var changedRemoteTaskRows = serverTasks.Where(
                x => x.ServerLastUpdated >= _dataState.LastSync).ToList();

            Console.WriteLine($"changedRemoteTaskRows count: {changedRemoteTaskRows.Count}");

            // handle local inserts/updates (originating from another client)
            foreach (var serverTask in changedRemoteTaskRows)
            {
                Console.WriteLine($"PullSync found changedRemoteTaskRow: {serverTask.TaskName}");
                DataSyncResult serverTaskResult = PullSyncableData(_mapper.Map<TaskDisplayModel>(serverTask));
                tasksPullResult = _dataHelper.CombineSyncResults(tasksPullResult, serverTaskResult);
            }

            // handle local deletions (originating from another client)
            DataSyncResult taskDeletionResult = await PullServerDataDeletions(ESyncableDataType.Task);
            tasksPullResult = _dataHelper.CombineSyncResults(tasksPullResult, taskDeletionResult); 

            if (changedRemoteTaskRows.Count == 0 && tasksPullResult.numRowsDeleted == 0)
            {
                Trace.WriteLine("serverTask - no changes detected on pull.");
                Console.WriteLine("serverTask - no changes detected on pull.");
            }

            // store copy for comparison + cleanup temp push history
            //_dataHelper.TasksLastFetch = serverTasks;
            //_dataState.WorkingTasks = serverTasks;
            _pushedTaskData.Clear();

            // trigger UI update if needed + add to total pull result
            if (_dataHelper.SyncChangesDetected(tasksPullResult))
            {
                _dataState.InvokeDataStateChanged("Tasks");
                pullResult = _dataHelper.CombineSyncResults(pullResult, tasksPullResult);
            }

            // PROJECT DATA
            DataSyncResult projectsPullResult = new();
            List<ProjectModel> serverProjects = await _projectEndpoint.GetAllProjectsForUser();
            var changedRemoteProjectRows = serverProjects.Where(
                x => x.ServerLastUpdated >= _dataState.LastSync).ToList();

            Console.WriteLine($"changedRemoteProjectRows count: {changedRemoteProjectRows.Count}");

            // handle local inserts/updates (originating from another client)
            foreach (var serverProject in changedRemoteProjectRows)
            {
                Console.WriteLine($"PullSync found changedRemoteProjectRow: {serverProject.ProjectName}");
                DataSyncResult serverProjectResult = PullSyncableData(_mapper.Map<ProjectDisplayModel>(serverProject));
                projectsPullResult = _dataHelper.CombineSyncResults(projectsPullResult, serverProjectResult);
            }

            // handle local deletions (originating from another client)
            DataSyncResult projectDeletionResult = await PullServerDataDeletions(ESyncableDataType.Project);
            projectsPullResult = _dataHelper.CombineSyncResults(projectsPullResult, projectDeletionResult);

            if (changedRemoteProjectRows.Count == 0 && projectsPullResult.numRowsDeleted == 0)
            {
                Trace.WriteLine("projects - no changes detected on pull.");
                Console.WriteLine("projects - no changes detected on pull.");
            }

            // store copy for comparison + cleanup temp push history
            //_dataHelper.ProjectsLastFetch = serverProjects;
            _pushedProjectData.Clear();

            // trigger UI update if needed + add to total pull result
            if (_dataHelper.SyncChangesDetected(projectsPullResult))
            {
                _dataState.InvokeDataStateChanged("Projects");
                pullResult = _dataHelper.CombineSyncResults(pullResult, projectsPullResult);
            }

            // CONTEXT DATA

            Console.WriteLine("Pull complete.");
            Trace.WriteLine("Pull complete.");
            //Console.WriteLine($"dataState.Tasks count: {_dataState.Tasks.Count}");
            return pullResult;
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
                        Console.WriteLine($"added local project on PullSyncableData: {displayServerProject.ProjectName}");
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
                    break;
                case ESyncableDataType.Settings:
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
                            Console.WriteLine($"PullServerDataDeletions found local row deleted on server: {clientDisplayProject.ProjectName}");
                            // ensure other projects have updated OrderIndex values
                            _dataService.ShiftCollectionOrderIndices(clientDisplayProject, _dataState.GetProjects()!);
                            _dataState.GetProjects()!.Remove(clientDisplayProject);
                            pullDeletionsResult.numRowsDeleted++;
                        }
                    }

                    break;

                case ESyncableDataType.Context:
                    break;
                default:
                    break;
            }

            return pullDeletionsResult;
        }
    }
}
