using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Data.Utilities;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Services
{
    public partial class DataService
    {
        private int _taskUpdateEntered = 0;
        private TaskDisplayModel? _taskBeingUpdated;

        // helper methods
        // ====================
        private void HandleTaskViewChanges(TaskDisplayModel task)
        {
            // if has both project and context, task is no longer in inbox
            if (task.InboxIndex != null && ((task.ProjectId != null && task.ContextId != null) || (task.CleanedUp)))
            {
                task.InboxIndex = null;
            }

            // if task is no longer in Today view
            if (task.TodayIndex != null && (
                (!task.Starred && !_dataHelper.IsTaskDueOrOverDue(task))) || // neither starred nor due/overdue
                (task.Completed && task.DateCompleted != DateTime.Now.Date) || // completed earlier than today
                (task.CleanedUp))
            {
                task.TodayIndex = null;
            }

            if (task.ProjectIndex != null && task.CleanedUp)
            {
                task.ProjectIndex = null;
            }

            if (task.ContextIndex != null && task.CleanedUp)
            {
                task.ContextIndex = null;
            }

            // if task should now be in Today view
            if (task.TodayIndex == null && (task.Starred || _dataHelper.IsTaskDueOrOverDue(task)) &&
                !(task.Completed && task.DateCompleted != DateTime.Now.Date))
            {
                task.TodayIndex = NextIndex(_dataState.GetTasks()!, x => x.TodayIndex);
            }
        }

        // the server records its own completion date, in UTC, once it receives the change
        private void HandleTaskCompletedChanged(TaskDisplayModel task)
        {
            if (task.Completed)
            {
                // only when it is checked off here, so an older completion keeps the date it has (or lacks)
                if (task.DateCompleted == null && !WasCompleted(task)) task.DateCompleted = DateTime.UtcNow;
                return;
            }

            task.DateCompleted = null;
            if (!task.CleanedUp) return;

            // cleaning up removed the task from its lists; return it to the end of each
            task.CleanedUp = false;
            if (task.ProjectId == null || task.ContextId == null)
            {
                task.InboxIndex = NextIndex(_dataState.GetTasks()!, x => x.InboxIndex);
            }
            if (task.ProjectId != null)
            {
                task.ProjectIndex = NextIndex(_dataState.GetTasks()!.Where(x => x.ProjectId == task.ProjectId), x => x.ProjectIndex);
            }
            if (task.ContextId != null)
            {
                task.ContextIndex = NextIndex(_dataState.GetTasks()!.Where(x => x.ContextId == task.ContextId), x => x.ContextIndex);
            }
        }

        private bool WasCompleted(TaskDisplayModel task) => task.Id == null
            ? _dataState.GetChangedTaskData().Find(x => x.TempLocalId == task.TempLocalId)?.Completed == true
            : _dataState.GetTasks()!.Find(x => x.Id == task.Id)?.Completed == true;

        private void HandleTaskProjectChanged(TaskDisplayModel task)
        {
            if (task.ProjectName == null) // project was unassigned
            {
                task.ProjectId = null;
                task.ProjectIndex = null;

                // if not already in inbox, will need InboxIndex assigned
                if (task.InboxIndex == null)
                {
                    task.InboxIndex = NextIndex(_dataState.GetTasks()!, x => x.InboxIndex);
                    //LogInformation($"{task.TaskName}: new InboxIndex is {task.InboxIndex}");
                }
            }
            else // has new assigned project
            {
                // lookup ProjectId by projectName and assign
                // note: unique project names are enforced on add/update
                ProjectDisplayModel? FindAssignedProject()
                {
                    List<ProjectDisplayModel> userProjects = _dataState.GetProjects()!.ToList();
                    return userProjects.Find(x => x.ProjectName == task.ProjectName);
                }

                ProjectDisplayModel? assignedProject = FindAssignedProject();
                if (assignedProject == null)
                {
                    // for now, do nothing; this should never be triggered
                    // leaving here in case in the future may change UI
                    // to allow user to directly assign and create at the same time

                    //ProjectModel newProject = new ProjectModel { ProjectName = task.ProjectName };
                    //assignedProject = AddProject(newProject);
                    //task.ProjectIndex = 0;
                    //task.ProjectId =  assignedProject!.Id;
                }
                else
                {
                    // determine project index for task
                    List<TaskDisplayModel> projectTasks = new();
                    if (assignedProject.Id != null)
                    {
                        projectTasks = _dataState.GetTasks()!
                            .Where(x => x.ProjectId == assignedProject.Id).ToList();
                        task.ProjectId = assignedProject!.Id;
                    }
                    //else
                    //{
                    //    projectTasks = _dataState.GetTasks()!
                    //        .Where(x => x.ProjectId == assignedProject.TempLocalId).ToList();
                    //    task.ProjectId = assignedProject!.TempLocalId;
                    //}

                    task.ProjectIndex = NextIndex(projectTasks, x => x.ProjectIndex);
                   // LogInformation($"{task.TaskName}: new ProjectId is {task.ProjectId}, projectName {task.ProjectName}");
                }           
            }
        }

        private void HandleTaskContextChanged(TaskDisplayModel task)
        {
            if (task.ContextName == null)  // context was unassigned
            {
                task.ContextId = null;
                task.ContextIndex = null;

                // if not already in inbox, will need InboxIndex assigned
                if (task.InboxIndex == null)
                {
                    task.InboxIndex = NextIndex(_dataState.GetTasks()!, x => x.InboxIndex);
                    //LogInformation($"{task.TaskName}: new InboxIndex is {task.InboxIndex}");
                }
            }
            else // has new assigned context
            {
                // lookup ContextId by contextName and assign
                // note: unique context names are enforced on add/update
                ContextDisplayModel? FindAssignedContext()
                {
                    List<ContextDisplayModel> userContexts = _dataState.GetContexts()!.ToList();
                    return userContexts.Find(x => x.ContextName == task.ContextName);
                }

                ContextDisplayModel? assignedContext = FindAssignedContext();
                if (assignedContext == null)
                {
                    // for now, do nothing; this should never be triggered
                    // leaving here in case in the future may change UI
                    // to allow user to directly assign and create at the same time

                    //ContextModel newContext = new ContextModel { ContextName = task.ContextName };
                    //assignedContext = AddContext(newContext);
                    //task.ContextIndex = 0;
                    //task.ContextId = assignedContext!.Id;
                }
                else
                {
                    // determine context index for task
                    List<TaskDisplayModel> contextTasks = new();
                    if (assignedContext.Id != null)
                    {
                        contextTasks = _dataState.GetTasks()!
                            .Where(x => x.ContextId == assignedContext.Id).ToList();
                        task.ContextId = assignedContext!.Id;
                    }

                    task.ContextIndex = NextIndex(contextTasks, x => x.ContextIndex);
                    LogInformation($"{task.TaskName}: new ContextId is {task.ContextId}, contextName {task.ContextName}");
                    //LogInformation($"{task.TaskName}: new ContextIndex is {task.ContextIndex}")
                }
            }
        }

        // Removing a task leaves a gap in the positions, so they only order a list: a new task goes after the highest.
        private static int NextIndex(IEnumerable<TaskDisplayModel> tasks, Func<TaskDisplayModel, int?> index) => (tasks.Max(index) ?? -1) + 1;

        // processes updated task data locally + flags for sync
        private void ProcessLocalTaskUpdate(TaskDataCompareResult compareResult, TaskDisplayModel workingTask)
        {
            if (!compareResult.IndicesOnly)
            {
                HandleTaskCompletedChanged(workingTask);
                // handle change of collection task belongs to
                if (compareResult.ProjectNameChanged) { HandleTaskProjectChanged(workingTask); }
                if (compareResult.ContextNameChanged) { HandleTaskContextChanged(workingTask); }
                // handle any adjustments to which view pages the task appears in
                HandleTaskViewChanges(workingTask);
            }

            // update data state Tasks object from WorkingTasks copy
            workingTask.ClientLastUpdated = DateTimeOffset.Now; // flag for sync
            if (workingTask.Id == null) // task hasn't yet been inserted on server; pending push
            {
                // update standard client data state copy
                var dataStateTask = _dataState.GetTasks()!.Find(x => x.TempLocalId == workingTask.TempLocalId);
                if (dataStateTask != null) dataStateTask.ValueAssign(workingTask);

                // update changedTaskData copy of task
                // note: only need to track pending property changes in ChangedTaskData if task has never been pushed
                var queuedChangedTask = _dataState.GetChangedTaskData()!.Find(x => x.TempLocalId == workingTask.TempLocalId);
                if (queuedChangedTask != null) queuedChangedTask.ValueAssign(workingTask);
            }
            else
            {
                // update standard client data state copy
                var dataStateTask = _dataState.GetTasks()!.Find(x => x.Id == workingTask.Id);
                if (dataStateTask != null) dataStateTask.ValueAssign(workingTask);

                // add copy to ChangedTaskData
                // don't duplicate if already had another update pending prior to push; update values instead
                var alreadyQueued = _dataState.GetChangedTaskData().Where(
                    x => x.Id == workingTask.Id);
                if (!alreadyQueued.Any())  _dataState.GetChangedTaskData().Add(workingTask.Clone()); 
                else alreadyQueued.FirstOrDefault()!.ValueAssign(workingTask);
            }
        }

        // updates local "working" copy of task data, for use after sync
        private void UpdateWorkingTasksFromDataState()
        {
            List<TaskDisplayModel> workingDisplayTaskList = _dataState.GetTasks()!.ConvertAll(task => task.Clone());
            _dataState.SetWorkingTasks(workingDisplayTaskList);
        }
    
        // a completed task is deleted once more whole days than the delete delay have passed since its completion;
        // completion dates are UTC, so the age is the same in every time zone and across daylight saving
        private static bool IsExpired(TaskDisplayModel task, int deleteDelayDays) =>
            task.Completed && task.DateCompleted is DateTime completed && (DateTime.UtcNow - completed).Days > deleteDelayDays;

        private TaskDisplayModel? QueuedChange(TaskDisplayModel task) => task.Id == null
            ? _dataState.GetChangedTaskData().Find(x => x.TempLocalId == task.TempLocalId)
            : _dataState.GetChangedTaskData().Find(x => x.Id == task.Id);

        // Each local edit gives the task and its queued change the same time. After signing in again, the task is the
        // server's row while an older change to it may still wait to be sent; an automatic change to the row would
        // overwrite that change, so it is left until the change is sent.
        private bool IsBehindQueuedChange(TaskDisplayModel task) => QueuedChange(task) is TaskDisplayModel queued && queued.ClientLastUpdated != task.ClientLastUpdated;

        // manages completed task cleanedUp status; for use prior to each sync (expired tasks are deleted during it)
        void IDataServiceInternal.PerformCompletedTaskCleanup()
        {
            // the settings as saved or waiting to be (the page's copy can hold an emptied field's 0, which is never saved)
            var userSettings = _dataState.GetUserSettings()!;
            // a changed delay applies once it is saved, so a change made later on another device isn't undercut by it
            var completedTasks = _dataState.GetChangedSettingsData() != null ? [] : _dataState.GetWorkingTasks()!.Where(x =>
                x.Completed && x.DateCompleted != null && !x.CleanedUp && !IsExpired(x, userSettings.DeleteDelayDays) && !IsBehindQueuedChange(x)).ToList();
            foreach (var workingTask in completedTasks)
            {
                int daysPassedSinceTaskCompletion = (DateTime.UtcNow - (DateTime)workingTask.DateCompleted!).Days;
                int cleanupIntervalSetting = userSettings.CleanUpDelayDays;
                if (userSettings.CleanUpImmediately || (daysPassedSinceTaskCompletion > cleanupIntervalSetting))
                {
                    workingTask.CleanedUp = true;
                    UpdateTaskData(workingTask);
                }
            }

            // remove TodayIndex from any completed (but not CleanedUp) tasks from view if completed > 1 day ago
            var oldCompletedTodayTasks = _dataState.GetWorkingTasks()!.Where(
                x => x.TodayIndex != null && x.Completed && (x.DateCompleted < DateTime.Now.Date) && !IsBehindQueuedChange(x)).ToList();
            foreach (TaskDisplayModel workingTask in oldCompletedTodayTasks)
            {
                // force update: will detect and remove TodayIndex, as well as shift other task indices accordingly if needed
                UpdateTaskData(workingTask, true);
            }
        }

        // Deletes completed tasks kept longer than the delete delay, on the server and here. It runs during a sync, after
        // this device's changes are sent and other devices' are pulled, so it uses the saved delay. A task with a change
        // still waiting to be sent is left for a later sync, and each one is checked against the server's row, which a
        // change this sync just sent (such as reopening it) may have updated. Nothing is queued: if a request fails, the
        // task is still on the server and the next sync tries again.
        async Task<DataSyncResult> IDataServiceInternal.DeleteExpiredTasks()
        {
            DataSyncResult result = new();
            // a delay changed during this sync applies once it is saved
            if (_dataState.GetChangedSettingsData() != null) return result;

            int deleteDelayDays = _dataState.GetUserSettings()!.DeleteDelayDays;
            var expiredTasks = _dataState.GetTasks()!.Where(x => x.Id != null && IsExpired(x, deleteDelayDays) && QueuedChange(x) == null).ToList();
            foreach (var task in expiredTasks)
            {
                TaskModel? serverTask = await _taskEndpoint.GetTaskById(task.Id!.Value);
                if (serverTask == null || !IsExpired(_mapper.Map<TaskDisplayModel>(serverTask), deleteDelayDays)) continue;

                await _taskEndpoint.DeleteTask(serverTask);
                _dataState.GetTasks()!.Remove(task);
                result.NumRowsDeleted++;
            }

            return result;
        }

        // manages today-view task time-sensitive auto deletion / cleanedUp status; for use prior to each sync
        void IDataServiceInternal.PerformTodayTaskCleanup()
        {
            var combinedTodayTasks = GetTodayTasks();

            // ensure any newly due/overdue tasks have a TodayIndex
            int nextTodayIndex = NextIndex(combinedTodayTasks, x => x.TodayIndex);
            foreach (TaskDisplayModel task in combinedTodayTasks)
            {
                if (task.DueDate <= DateTime.Now.Date && task.TodayIndex == null)
                {
                    task.TodayIndex = nextTodayIndex;
                    UpdateTaskData(task, true);
                    nextTodayIndex++;
                }
            }
        }

        public List<TaskDisplayModel> GetTodayTasks() => _dataHelper.GetTodayWorkingTasks();

        public bool IsTaskCurrentlyBeingUpdated(TaskDisplayModel task)
        {
            if (_taskBeingUpdated != null)
            {
                if (task.Id == null && task.TempLocalId != null)
                {
                    return _taskBeingUpdated.TempLocalId == task.TempLocalId;
                }
                else { return _taskBeingUpdated.Id == task.Id; }
            }

            return false;
        }

        // data state CRUD operations
        // ====================

        // for front-end access to local data state
        public List<TaskDisplayModel>? GetDataStateTasks() => _dataState.GetWorkingTasks();

        // for populating local data state
        public async Task FetchRemoteTaskData()
        {
            var taskList = await _taskEndpoint.GetAllTasksForUser();
            var displayTaskList = _mapper.Map<List<TaskDisplayModel>>(taskList);
            _dataState.SetTasks(displayTaskList);
            UpdateWorkingTasksFromDataState();

            _dataState.InvokeDataStateChanged(nameof(EDataRefreshType.Tasks));
        }

        // validates request, processes local add, flags for sync, refreshes UI
        public void AddTask(TaskDisplayModel workingTask)
        {
            if (string.IsNullOrWhiteSpace(workingTask.TaskName)) { return; }

            if (workingTask.ProjectName == null || workingTask.ContextName == null)
            {
                workingTask.InboxIndex = NextIndex(_dataState.GetTasks()!, x => x.InboxIndex);
            }

            if (workingTask.ProjectName != null) HandleTaskProjectChanged(workingTask);
            if (workingTask.ContextName != null) HandleTaskContextChanged(workingTask);

            if (workingTask.Starred || _dataHelper.IsTaskDueOrOverDue(workingTask))
            {
                workingTask.TodayIndex = NextIndex(_dataState.GetTasks()!, x => x.TodayIndex);
            }

            // give temp local tracking id
            var currentTempId = _dataState.GetTempTaskId();
            workingTask.TempLocalId = _dataState.SetTempTaskId(++currentTempId);

            // flag for sync
            workingTask.ClientLastUpdated = DateTimeOffset.Now;
            _dataState.GetChangedTaskData().Add(workingTask.Clone());

            // update local data state
            _dataState.GetWorkingTasks()!.Add(workingTask.Clone());
            _dataState.GetTasks()!.Add(workingTask.Clone());

            // trigger UI update
            _dataState.InvokeDataStateChanged(nameof(EDataRefreshType.Tasks));
        }

        // validates request, processes local delete, flags for sync, refreshes UI
        public void DeleteTask(TaskDisplayModel workingTask)
        {
            // update local data state
            if (workingTask.Id == null) // never existed on server; insert was pending push
            {
                // local delete
                var dataStateTask = _dataState.GetTasks()!.Find(x => x.TempLocalId == workingTask.TempLocalId);
                if (dataStateTask != null) _dataState.GetTasks()!.Remove(dataStateTask);
                var changedTask = _dataState.GetChangedTaskData().Find(x => x.TempLocalId == workingTask.TempLocalId);
                if (changedTask != null) _dataState.GetChangedTaskData().Remove(changedTask!);
                _dataState.GetWorkingTasks()!.Remove(workingTask);
            }
            else
            {
                TaskDisplayModel dataStateTask = _dataState.GetTasks()!.Find(x => x.Id == workingTask.Id)!;
                // flag for server delete on sync
                dataStateTask.Deleted = DateTimeOffset.Now;
                dataStateTask.ClientLastUpdated = DateTimeOffset.Now;

                // if had an update pending push, turn it into the delete instead of adding a duplicate to changedTaskData
                // only mark it: a push may already be sending its values
                var queuedTask = _dataState.GetChangedTaskData().Find(x => x.Id == dataStateTask.Id);
                if (queuedTask == null) _dataState.GetChangedTaskData().Add(dataStateTask.Clone());
                else queuedTask.Deleted = dataStateTask.Deleted;

                // local delete
                if (dataStateTask != null) _dataState.GetTasks()!.Remove(dataStateTask);
                _dataState.GetWorkingTasks()!.Remove(workingTask);
            }

            // trigger UI update
            _dataState.InvokeDataStateChanged(nameof(EDataRefreshType.Tasks));
        }

        // validates request, performs additional processing, refreshes UI
        public void UpdateTaskData(TaskDisplayModel workingTask, bool forceUpdate = false)
        {
            TaskDataCompareResult compareResult = _dataHelper.HasTaskDataChanged(workingTask);

            if (compareResult.HasChanged || forceUpdate)
            {
                if (_taskBeingUpdated != null)
                {
                    if (workingTask.Id == null && workingTask.TempLocalId != _taskBeingUpdated.TempLocalId ||
                        workingTask.Id != _taskBeingUpdated.Id)
                    {
                        // unlock
                        Interlocked.Exchange(ref _taskUpdateEntered, 0);
                        _taskBeingUpdated = null;
                    }
                }

                // lock, to prevent other property changes during ProcessLocalTaskUpdate from triggering new Update calls
                if (Interlocked.Increment(ref _taskUpdateEntered) != 1) { return; }
                _taskBeingUpdated = workingTask;

                // do any additional required processing (updating indices, etc)
                // as well as update non-working local copies + flag for sync
                ProcessLocalTaskUpdate(compareResult, workingTask);

                // unlock
                Interlocked.Exchange(ref _taskUpdateEntered, 0);
                _taskBeingUpdated = null;

                // trigger UI update
                _dataState.InvokeDataStateChanged(nameof(EDataRefreshType.Tasks));
            }
        }
    }
}
