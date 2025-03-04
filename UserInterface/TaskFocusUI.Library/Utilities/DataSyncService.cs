using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Utilities
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
            Console.WriteLine($"InitSync complete; DataState populated.");
            Trace.WriteLine($"InitSync complete; DataState populated.");
        }

        // syncs at row level; LastUpdated determines who wins at the server
        public async Task Sync()
        {
            // debug
            Console.WriteLine($"starting Sync... (lastSync prior: {_dataState.LastSync}");
            Trace.WriteLine($"starting Sync... (lastSync prior: {_dataState.LastSync}");

            // push all client rows locally changed since last sync to the server
            int[] numPushedRows = await PushSync();
            // pull all records that have changed since last sync...
            // and update the local client data with any changes (inserts, deletions, updates)
            int[] numPulledRows = await PullSync();

            _dataState.LastSync = DateTimeOffset.Now;

            Console.WriteLine($"Sync complete!");
            Console.WriteLine($"PUSH: {numPushedRows[0]} rows inserted, {numPushedRows[1]} rows deleted, {numPushedRows[2]} rows updated.");
            Console.WriteLine($"PULL: {numPulledRows[0]} rows inserted, {numPulledRows[1]} rows deleted, {numPulledRows[2]} rows updated.");
            Trace.WriteLine($"Sync complete!");
            Trace.WriteLine($"PUSH: {numPushedRows[0]} rows inserted, {numPushedRows[1]} rows deleted, {numPushedRows[2]} rows updated.");
            Trace.WriteLine($"PULL: {numPulledRows[0]} rows inserted, {numPulledRows[1]} rows deleted, {numPulledRows[2]} rows updated.");
        }

        private async Task<int[]> PushSync()
        {
            Console.WriteLine("Pushing changes to server...");
            Trace.WriteLine("Pushing changes to server...");
            int numRowsInserted = 0;
            int numRowsDeleted = 0;
            int numRowsUpdated = 0;

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

            // USER DATA (name only; email/pw are handled separately from data
            foreach (var clientUser in _dataState.ChangedUserData)
            {
                UserModel serverUser = await _userEndpoint.GetCurrentUserData();
                if (serverUser.ClientLastUpdated > clientUser.ServerLastUpdated)
                {
                    // conflict - server data is newer than client
                    // server wins; ignore changes and just update time
                    serverUser.ServerLastUpdated = DateTimeOffset.Now;
                    serverUser.ClientLastUpdated = clientUser.ServerLastUpdated;
                    await _userEndpoint.UpdateName(serverUser);

                    Console.WriteLine("Push conflict detected! Local changes ignored; only time updated");
                    Trace.WriteLine("Push conflict detected! Local changes ignored; only time updated");
                }
                else // client data is newer than server
                {
                    Console.WriteLine($"clientUser.ServerLastUpdated: {clientUser.ServerLastUpdated}, clientUser.ClientLastUpdated: {clientUser.ClientLastUpdated}");
                    Trace.WriteLine($"clientUser.ServerLastUpdated: {clientUser.ServerLastUpdated}, clientUser.ClientLastUpdated: {clientUser.ClientLastUpdated}");
                    clientUser.ServerLastUpdated = DateTimeOffset.Now;
                    await _userEndpoint.UpdateName(clientUser);
                    _pushedUserData.Add(clientUser);
                    numRowsUpdated++;
                }
            }
            _dataState.ChangedUserData.Clear();

            //// SETTINGS DATA
            //foreach (var row in _dataState.ChangedUserSettingsData)
            //    await _userEndpoint.UpdateUserSettings(row);
            //_dataState.ChangedUserSettingsData.Clear();

            //// TASK DATA - NEEDS UPDATING FROM LATEST ^ USER DATA SECTION LOGIC ADJUSTMENTS
            foreach (var clientTask in _dataState.ChangedTaskData)
            {
                // check task exists // how to check this efficiently? may need to make a CheckTaskExists call if this doesn't return a null value...
                int taskId = (int)clientTask.Id!;
                TaskModel serverTask = await _taskEndpoint.GetTaskById(taskId);

                if (serverTask == null) // insert
                {
                    if (clientTask.ServerLastUpdated == DateTimeOffset.MinValue)
                        clientTask.ServerLastUpdated = DateTimeOffset.Now;
                    await _taskEndpoint.AddTask(clientTask, clientTask.UserId);
                    _pushedTaskData.Add(clientTask);
                    numRowsInserted++;
                }
                else if (clientTask.Deleted.HasValue) // delete
                {
                    await _taskEndpoint.DeleteTask(serverTask);
                    numRowsDeleted++;
                }
                else // update
                {
                    if (serverTask.ClientLastUpdated > clientTask.ServerLastUpdated)
                    {
                        // conflict - server data is newer than client
                        // server wins; ignore changes and just update time
                        serverTask.ServerLastUpdated = DateTimeOffset.Now;
                        serverTask.ClientLastUpdated = clientTask.ServerLastUpdated;
                        await _taskEndpoint.UpdateTask(serverTask);

                        Console.WriteLine("Push conflict detected! Local changes ignored; only time updated");
                        Trace.WriteLine("Push conflict detected! Local changes ignored; only time updated");
                    }
                    else // client data is newer than server
                    {
                        Console.WriteLine($"clientTask.ServerLastUpdated: {clientTask.ServerLastUpdated}, clientTask.ClientLastUpdated: {clientTask.ClientLastUpdated}");
                        Trace.WriteLine($"clientTask.ServerLastUpdated: {clientTask.ServerLastUpdated}, clientTask.ClientLastUpdated: {clientTask.ClientLastUpdated}");

                        clientTask.ServerLastUpdated = DateTimeOffset.Now;
                        await _taskEndpoint.UpdateTask(clientTask);
                        _pushedTaskData.Add(clientTask);
                        numRowsUpdated++;
                    }
                }
            }
            _dataState.ChangedTaskData.Clear();

            //// PROJECT DATA
            //foreach (var row in _dataState.ChangedProjectData)
            //    await _projectEndpoint.UpdateProject(row);
            //_dataState.ChangedProjectData.Clear();
            //// CONTEXT DATA
            //foreach (var row in _dataState.ChangedContextData)
            //    await _contextEndpoint.UpdateContext(row);
            //_dataState.ChangedContextData.Clear();

            Trace.WriteLine("Push complete.");
            Console.WriteLine("Push complete.");
            return [numRowsInserted, numRowsDeleted, numRowsUpdated];
        }

        private async Task<int[]> PullSync()
        {
            Console.WriteLine("Pulling changes from server...");
            int numRowsInserted = 0;
            int numRowsDeleted = 0;
            int numRowsUpdated = 0;

            // pull sync is just getting all records that have changed since that LastSync...
            // ... and updating the local client data with any changes (inserts, deletions, updates)

            // USER DATA
            UserModel serverCurrentUser = await _userEndpoint.GetCurrentUserData();
            IList<UserModel> serverUserRows = [serverCurrentUser];
            var changedRemoteUserRows = serverUserRows.Where(
                x => x.ServerLastUpdated >= _dataState.LastSync).ToList();

            foreach (var serverUser in changedRemoteUserRows)
            {
                // do not pull if we just pushed the change
                var pushedServerUser = _pushedUserData.Where(x => x.Id == serverUser.Id);
                if (pushedServerUser.Count() != 0)
                {
                    Trace.WriteLine("continue...");
                    Console.WriteLine("continue...");
                    continue;
                }

                Trace.WriteLine("serverCurrentUser - changes detected on pull!");
                Trace.WriteLine($"serverUser.ServerLastUpdated: {serverUser.ServerLastUpdated}; LastSync: {_dataState.LastSync}; a >= b: {serverUser.ServerLastUpdated >= _dataState.LastSync}");
                Console.WriteLine("serverCurrentUser - changes detected on pull!");
                Console.WriteLine($"serverUser.ServerLastUpdated: {serverUser.ServerLastUpdated}; LastSync: {_dataState.LastSync}; a >= b: {serverUser.ServerLastUpdated >= _dataState.LastSync}");

                UserDisplayModel displayServerUser = _mapper.Map<UserDisplayModel>(serverUser);
                displayServerUser.ServerLastUpdated = DateTimeOffset.Now;
                // update local data store + store copy for comparison
                _dataState.CurrentUser = displayServerUser;
                //_dataHelper.UserDataLastFetch = _dataState.CurrentUser;
                numRowsUpdated++;
            }

            if (changedRemoteUserRows.Count == 0)
            {
                Trace.WriteLine("serverCurrentUser - no changes detected on pull.");
                Trace.WriteLine($"serverUser.ServerLastUpdated: {serverCurrentUser.ServerLastUpdated}; LastSync: {_dataState.LastSync}; a >= b: {serverCurrentUser.ServerLastUpdated >= _dataState.LastSync}");
                Console.WriteLine("serverCurrentUser - no changes detected on pull.");
                Console.WriteLine($"serverUser.ServerLastUpdated: {serverCurrentUser.ServerLastUpdated}; LastSync: {_dataState.LastSync}; a >= b: {serverCurrentUser.ServerLastUpdated >= _dataState.LastSync}");
            }

            _pushedUserData.Clear();

            // SETTINGS DATA

            // TASK DATA
            List<TaskModel> serverTasks = await _taskEndpoint.GetAllTasksForUser();
            var changedRemoteTaskRows = serverTasks.Where(
                x => x.ServerLastUpdated >= _dataState.LastSync).ToList();

            foreach (var serverTask in changedRemoteTaskRows)
            {
                // do not pull if we just pushed the change
                var pushedServerTask = _pushedTaskData.Where(x => x.Id == serverTask.Id);
                if (pushedServerTask.Count() != 0)
                {
                    Trace.WriteLine("continue...");
                    Console.WriteLine("continue...");
                    continue;
                }

                Trace.WriteLine("serverTask - changes detected on pull!");
                //Trace.WriteLine($"serverTask.ServerLastUpdated: {serverTask.ServerLastUpdated}; LastSync: {_dataState.LastSync}; a >= b: {serverTask.ServerLastUpdated >= _dataState.LastSync}");
                Console.WriteLine("serverTask - changes detected on pull!");
                //Console.WriteLine($"serverTask.ServerLastUpdated: {serverTask.ServerLastUpdated}; LastSync: {_dataState.LastSync}; a >= b: {serverTask.ServerLastUpdated >= _dataState.LastSync}");

                TaskDisplayModel displayServerTask = _mapper.Map<TaskDisplayModel>(serverTask);
                TaskDisplayModel? clientTask = _dataState.Tasks!.Find(x => x.Id == displayServerTask.Id);

                if (clientTask == null) // insert
                {
                    _dataState.Tasks.Add(displayServerTask.Clone());
                    numRowsInserted++;
                }
                // DELETE: this won't work, as Deleted isn't tracked server-side. 
                // if the task has been deleted by another client app and then synced to the server, it's GONE.
                // you could do the same thing server side we're doing per-client side, meaning, instead of deleting
                // right away, flag as Deleted and wait until sync to actually process that deletion.
                // the problem is, server side, you won't know when all clients have synced that deletion.
                // the reason for this is there could be any number of clients! 
                // you'd need to implement some kind of per-client incremental unique ID tracking system
                // for server-side to keep up with all clients its ever synced with, and add all changed rows
                // on each client sync to a table (ChangedUnpulledTaskData, with a column for ClientID ??)
                // and then mimic the same behavior (instead of serverTasks = await _taskEndpoint.GetAllTasksForUser(); on Pull,
                // you'd have a call for like GetChangedUnpulledTaskData(string clientID) returning that client's changed rows only).
                // ALL THIS is messy and would only server to resolve conflicts that are very unlikely to happen if we just
                // instead push and then pull and trust that if we have any rows the server doesn't at that point, delete them locally.
                // i.e., server wins on deletions from other clients, no questions.
                //else if (serverTask.Deleted.HasValue) // delete
                //{
                //    _dataState.Tasks.Remove(clientTask);
                //}
                else // update
                { 
                    clientTask = displayServerTask;
                    numRowsUpdated++;
                }
            }

            // TASKS - PULL DETECTED SERVER-SIDE DELETIONS (meaning, originating from another client)
            foreach (var clientDisplayTask in _dataState.Tasks!)
            {
                TaskModel? serverTask = serverTasks.Find(x => x.Id == clientDisplayTask.Id);
                if (serverTask == null) // not found on server? delete locally
                {
                    _dataState.Tasks.Remove(clientDisplayTask);
                    numRowsDeleted++;
                }
            }

            if (changedRemoteTaskRows.Count == 0 && numRowsDeleted == 0)
            {
                Trace.WriteLine("serverTask - no changes detected on pull.");
                Console.WriteLine("serverTask - no changes detected on pull.");
            }

            // store copy for comparison + cleanup temp push history
            _dataHelper.TasksLastFetch = serverTasks;
            _pushedTaskData.Clear();

            // PROJECT DATA

            // CONTEXT DATA

            Console.WriteLine("Pull complete.");
            Trace.WriteLine("Pull complete.");
            return [numRowsInserted, numRowsDeleted, numRowsUpdated];
        }
    }
}
