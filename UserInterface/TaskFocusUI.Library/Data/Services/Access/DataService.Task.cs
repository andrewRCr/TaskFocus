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
                ShiftTaskCollectionSourceIndices(task, "InboxIndex");
                task.InboxIndex = null;
            }

            // if task is no longer in Today view
            if (task.TodayIndex != null && (
                (!task.Starred && !_dataHelper.IsTaskDueOrOverDue(task))) || // neither starred nor due/overdue
                (task.Completed && task.DateCompleted != DateTime.Now.Date) || // completed earlier than today
                (task.CleanedUp))
            {
                ShiftTaskCollectionSourceIndices(task, "TodayIndex");
                task.TodayIndex = null;
            }

            if (task.ProjectIndex != null && task.CleanedUp)
            {
                ShiftTaskCollectionSourceIndices(task, "ProjectIndex");
                task.ProjectIndex = null;
            }

            if (task.ContextIndex != null && task.CleanedUp)
            {
                ShiftTaskCollectionSourceIndices(task, "ContextIndex");
                task.ContextIndex = null;
            }

            // if task should now be in Today view
            if (task.TodayIndex == null && (task.Starred || _dataHelper.IsTaskDueOrOverDue(task)) &&
                !(task.Completed && task.DateCompleted != DateTime.Now.Date))
            {
                //List<TaskDisplayModel> starredTasks = _dataState.Tasks!
                //    .Where(x => x.Starred).ToList();

                List<TaskDisplayModel> starredTasks = _dataState.GetTasks()!
                    .Where(x => x.Starred).ToList();

                //List<TaskDisplayModel> dueTasks = _dataState.Tasks!
                //    .Where(x => x.DueDate <= DateTime.Now.Date).ToList();

                List<TaskDisplayModel> dueTasks = _dataState.GetTasks()!
                    .Where(x => x.DueDate <= DateTime.Now.Date).ToList();

                List<TaskDisplayModel> todayTasks = dueTasks.Concat(starredTasks).ToList();
                var pushedTodayTasks = todayTasks.Where(x => x.Id != null).ToList();
                pushedTodayTasks = pushedTodayTasks.DistinctBy(x => x.Id).ToList();
                //Console.WriteLine($"pushedTodayTasks count: {pushedTodayTasks.Count}, contents:");
                //foreach (var item in pushedTodayTasks)
                //{
                //    Console.WriteLine($"{item.TaskName}, {item.Id}");
                //}

                var unpushedTodayTasks = todayTasks.Where(x => x.Id == null).ToList();
                unpushedTodayTasks = unpushedTodayTasks.DistinctBy(x => x.TempLocalId).ToList();
                //Console.WriteLine($"unpushedTodayTasks count: {unpushedTodayTasks.Count}, contents: ");

                //foreach (var item in unpushedTodayTasks)
                //{
                //    Console.WriteLine($"{item.TaskName}, {item.TempLocalId}");
                //}


                todayTasks = pushedTodayTasks.Concat(unpushedTodayTasks).ToList();
                //Console.WriteLine($"todayTasks count: {todayTasks.Count}");

                //task.TodayIndex = todayTasks.Count > 0 ? (todayTasks.Count) : 0;
                task.TodayIndex = todayTasks.Count;
                //Console.WriteLine($"newly assigned TodayIndex: {task.TodayIndex}");
            }
        }

        private void HandleTaskProjectChanged(TaskDisplayModel task)
        {
            ShiftTaskCollectionSourceIndices(task, "ProjectIndex");

            if (task.ProjectName == null) // project was unassigned
            {
                task.ProjectId = null;
                task.ProjectIndex = null;

                // if not already in inbox, will need InboxIndex assigned
                if (task.InboxIndex == null)
                {
                    List<TaskDisplayModel> inboxTasks = _dataState.GetTasks()!
                        .Where(x => x.ProjectId == null || x.ContextId == null).ToList();

                    task.InboxIndex = inboxTasks.Count;
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

                    task.ProjectIndex = projectTasks.Count;
                    LogInformation($"{task.TaskName}: new ProjectId is {task.ProjectId}, projectName {task.ProjectName}");
                    //LogInformation($"{task.TaskName}: new ProjectIndex is {task.ProjectIndex}");
                }           
            }
        }

        private void HandleTaskContextChanged(TaskDisplayModel task)
        {
            ShiftTaskCollectionSourceIndices(task, "ContextIndex");

            if (task.ContextName == null)  // context was unassigned
            {
                task.ContextId = null;
                task.ContextIndex = null;

                // if not already in inbox, will need InboxIndex assigned
                if (task.InboxIndex == null)
                {
                    List<TaskDisplayModel> inboxTasks = _dataState.GetTasks()!
                        .Where(x => x.ProjectId == null || x.ContextId == null).ToList();

                    task.InboxIndex = inboxTasks.Count;
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

                    task.ContextIndex = contextTasks.Count;
                    LogInformation($"{task.TaskName}: new ContextId is {task.ContextId}, contextName {task.ContextName}");
                    //LogInformation($"{task.TaskName}: new ContextIndex is {task.ContextIndex}")
                }
            }
        }

        // for use when removing a task *from* the inbox or a project/context
        private void ShiftTaskCollectionSourceIndices(TaskDisplayModel task, string indexType)
        {
            int? previouslyAssignedCollectionIndex = null;
            int? previouslyAssignedCollectionId = null;
            List<TaskDisplayModel>? previousCollectionTasks = null;

            switch (indexType)
            {
                case "InboxIndex": // task is being moved out of the inbox
                    previouslyAssignedCollectionIndex = task.InboxIndex;
                    previousCollectionTasks = _dataState.GetTasks()!
                        .Where(x => x.ProjectId == null || x.ContextId == null).ToList();
                    foreach (TaskDisplayModel previousCollectionTask in previousCollectionTasks)
                    {
                        bool shiftNeeded = previousCollectionTask.InboxIndex > previouslyAssignedCollectionIndex;
                        if (shiftNeeded) { previousCollectionTask.InboxIndex--; }
                    }

                    break;

                case "ProjectIndex": // task is being moved out of an existing project
                    if (task.ProjectId != null || task.ContextId != null)
                    {
                        previouslyAssignedCollectionIndex = task.ProjectIndex;
                        previouslyAssignedCollectionId = task.ProjectId;
                        previousCollectionTasks = _dataState.GetTasks()!
                            .Where(x => x.ProjectId == previouslyAssignedCollectionId).ToList();
                        foreach (TaskDisplayModel previousCollectionTask in previousCollectionTasks)
                        {
                            bool shiftNeeded = previousCollectionTask.ProjectIndex > previouslyAssignedCollectionIndex;
                            if (shiftNeeded) { previousCollectionTask.ProjectIndex--; }
                        }
                    }
                    break;

                case "ContextIndex": // task is being moved out of an existing context
                    if (task.ContextId != null || task.ProjectId != null)
                    {
                        previouslyAssignedCollectionIndex = task.ContextIndex;
                        previouslyAssignedCollectionId = task.ContextId;
                        previousCollectionTasks = _dataState.GetTasks()!
                            .Where(x => x.ContextId == previouslyAssignedCollectionId).ToList();
                        foreach (TaskDisplayModel previousCollectionTask in previousCollectionTasks)
                        {
                            bool shiftNeeded = previousCollectionTask.ContextIndex > previouslyAssignedCollectionIndex;
                            if (shiftNeeded) { previousCollectionTask.ContextIndex--; }
                        }
                    }
                    break;

                case "TodayIndex": // task is having its Starred prop set to False / due date changed to no longer due/overdue
                    if (!task.Starred || !_dataHelper.IsTaskDueOrOverDue(task))
                    {
                        previouslyAssignedCollectionIndex = task.TodayIndex;
                        List<TaskDisplayModel> dueTasks = _dataState.GetTasks()!
                            .Where(x => x.DueDate <= DateTime.Now.Date).ToList();
                        List<TaskDisplayModel> starredTasks = _dataState.GetTasks()!
                            .Where(x => x.Starred).ToList();
                        previousCollectionTasks = dueTasks.Concat(starredTasks).ToList();

                        var pushedPreviousCollectionTasks = previousCollectionTasks.Where
                            (x => x.Id != null).ToList();
                        var unpushedPreviousCollectionTasks = previousCollectionTasks.Where
                            (x => x.Id == null).ToList();

                        pushedPreviousCollectionTasks = pushedPreviousCollectionTasks.DistinctBy(x => x.Id).ToList();
                        //Console.WriteLine($"pushedPrevCollectionTasks count: {pushedPreviousCollectionTasks.Count}");
                        unpushedPreviousCollectionTasks = unpushedPreviousCollectionTasks.DistinctBy(x => x.TempLocalId).ToList();
                        //Console.WriteLine($"UNpushedPrevCollectionTasks count: {unpushedPreviousCollectionTasks.Count}");
                        previousCollectionTasks = pushedPreviousCollectionTasks.Concat(unpushedPreviousCollectionTasks).ToList();
                        //Console.WriteLine($"total final concat prevCollectionTasks count: {previousCollectionTasks.Count}");

                        foreach (TaskDisplayModel previousCollectionTask in previousCollectionTasks)
                        {
                            bool shiftNeeded = previousCollectionTask.TodayIndex > previouslyAssignedCollectionIndex;
                            if (shiftNeeded) { previousCollectionTask.TodayIndex--; }
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        // processes updated task data locally + flags for sync
        private void ProcessLocalTaskUpdate(TaskDataCompareResult compareResult, TaskDisplayModel workingTask)
        {
            if (!compareResult.IndicesOnly)
            {
                // handle change of collection task belongs to
                if (compareResult.ProjectNameChanged) { HandleTaskProjectChanged(workingTask); }
                if (compareResult.ContextNameChanged) { HandleTaskContextChanged(workingTask); }
                // handle any adjustments to which view pages the task appears in
                HandleTaskViewChanges(workingTask);

                //LogInformation(workingTask.ProjectId.ToString());
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
                // don't duplicate if already had another update pending prior to push
                var alreadyQueued = _dataState.GetChangedTaskData().Where(
                    x => x.Id == workingTask.Id);
                if (!alreadyQueued.Any()) { _dataState.GetChangedTaskData().Add(workingTask.Clone()); }
            }

            var dataStateTask1 = _dataState.GetTasks()!.Find(x => x.Id == workingTask.Id);
            var dataStateWorkingTask = _dataState.GetWorkingTasks()!.Find(x => x.Id == workingTask.Id);
            LogInformation($"at processLocalTaskUpdate end, dataStateTask projectId: {dataStateTask1.ProjectId}, dataStateWorkingTask projectId: {dataStateWorkingTask.ProjectId}");
        }

        // updates local "working" copy of task data, for use after sync
        private void UpdateWorkingTasksFromDataState()
        {
            List<TaskDisplayModel> workingDisplayTaskList = _dataState.GetTasks()!.ConvertAll(task => task.Clone());
            _dataState.SetWorkingTasks(workingDisplayTaskList);
        }
    
        // manages completed task time-sensitive auto deletion / cleanedUp status; for use prior to each sync
        void IDataServiceInternal.PerformCompletedTaskCleanup()
        {
            var userSettings = _dataState.GetWorkingUserSettings()!;
            var completedTasks = _dataState.GetWorkingTasks()!.Where(x => x.Completed);
            foreach (var workingTask in completedTasks)
            {
                TimeSpan interval = (DateTime.Now - (DateTime)workingTask.DateCompleted!);
                int daysPassedSinceTaskCompletion = interval.Days;

                // delete if interval passed
                int deleteIntervalSetting = userSettings.DeleteDelayDays;
                if (daysPassedSinceTaskCompletion > deleteIntervalSetting) DeleteTask(workingTask);
      
                else if (!workingTask.CleanedUp) // handle CleanedUp state
                {
                    int cleanupIntervalSetting = userSettings.CleanUpDelayDays;
                    if (userSettings.CleanUpImmediately || (daysPassedSinceTaskCompletion > cleanupIntervalSetting))
                    {
                        workingTask.CleanedUp = true;
                        UpdateTaskData(workingTask);
                    }
                }          
            }

            // remove TodayIndex from any completed (but not CleanedUp) tasks from view if completed > 1 day ago
            var oldCompletedTodayTasks = _dataState.GetWorkingTasks()!.Where(
                x => x.TodayIndex != null && x.Completed && (x.DateCompleted < DateTime.Now.Date)).ToList();
            foreach (TaskDisplayModel workingTask in oldCompletedTodayTasks)
            {
                // force update: will detect and remove TodayIndex, as well as shift other task indices accordingly if needed
                UpdateTaskData(workingTask, true);
            }
        }

        void IDataServiceInternal.HandleIndexShiftsOnTaskDeletion(TaskDisplayModel task)
        {
            if (task.ProjectId == null || task.ContextId == null)
            {
                ShiftTaskCollectionSourceIndices(task, "InboxIndex");
            }
            if (task.ProjectId != null)
            {
                ShiftTaskCollectionSourceIndices(task, "ProjectIndex");
            }
            if (task.ContextId != null)
            {
                ShiftTaskCollectionSourceIndices(task, "ContextIndex");
            }
            if (task.Starred)
            {
                ShiftTaskCollectionSourceIndices(task, "TodayIndex");
            }
        }

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

            //List<TaskDisplayModel> workingDisplayTaskList = displayTaskList.ConvertAll(task => task.Clone());
            //_dataState.SetWorkingTasks(workingDisplayTaskList);
            UpdateWorkingTasksFromDataState();

            _dataState.InvokeDataStateChanged(nameof(EDataRefreshType.Tasks));
        }

        // validates request, processes local add, flags for sync, refreshes UI
        public async Task AddTask(TaskDisplayModel workingTask)
        {
            if (string.IsNullOrWhiteSpace(workingTask.TaskName)) { return; }

            if (workingTask.ProjectName == null || workingTask.ContextName == null)
            {
                List<TaskDisplayModel> inboxTasks = _dataState.GetTasks()!
                    .Where(x => x.ProjectId == null || x.ContextId == null).ToList();

                workingTask.InboxIndex = inboxTasks.Count;
                //LogInformation($"{task.TaskName}: new InboxIndex is {task.InboxIndex}");
            }

            if (workingTask.ProjectName != null) HandleTaskProjectChanged(workingTask);
            if (workingTask.ContextName != null) HandleTaskContextChanged(workingTask);

            // give temp local tracking id
            var currentTempId = _dataState.GetTempTaskId();
            workingTask.TempLocalId = _dataState.SetTempTaskId(++currentTempId);


            // flag for sync
            workingTask.ClientLastUpdated = DateTimeOffset.Now;
            _dataState.GetChangedTaskData().Add(workingTask.Clone());
            foreach (var task in _dataState.GetChangedTaskData())
            {
                Console.WriteLine($"changedTaskData contents: {task.TaskName}, tempLocalId: {task.TempLocalId}");
            }

            // update local data state
            _dataState.GetWorkingTasks()!.Add(workingTask.Clone());
            _dataState.GetTasks()!.Add(workingTask.Clone());

            // trigger UI update
            _dataState.InvokeDataStateChanged(nameof(EDataRefreshType.Tasks));
        }

        // validates request, processes local delete, flags for sync, refreshes UI
        public void DeleteTask(TaskDisplayModel workingTask)
        {
            this.HandleIndexShiftsOnTaskDeletion(workingTask);

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

                // if had an update pending push, don't add a duplicate to changedTaskData
                var alreadyQueued = _dataState.GetChangedTaskData().Where(
                    x => x.Id == dataStateTask.Id);
                if (alreadyQueued.Count() == 0) { _dataState.GetChangedTaskData().Add(dataStateTask.Clone()); }

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

            Console.WriteLine($"compareResult.HasChanged: {compareResult.HasChanged}");
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

                Console.WriteLine("1 task update complete");
                // trigger UI update
                _dataState.InvokeDataStateChanged(nameof(EDataRefreshType.Tasks));
            }
        }

        // alternate update method - updates entire task group (arbitrary by view) prior to refreshing UI
        //public void UpdateTaskViewOrderingIndices(List<TaskDisplayModel> displayTasks)
        //{
        //    foreach (TaskDisplayModel displayTask in displayTasks)
        //    {
        //        TaskModel task = _mapper.Map<TaskModel>(displayTask);
        //        TaskDataCompareResult compareResult = _dataHelper.HasTaskDataChanged(displayTask);
        //        // only update if changed
        //        if (compareResult.HasChanged)
        //        {
        //            // update local data state
        //            TaskDisplayModel updatedDisplayTask = _mapper.Map<TaskDisplayModel>(task);

        //            if (updatedDisplayTask.Id != null)
        //            {
        //                TaskDisplayModel clientTask = _dataState.GetTasks()!.Find(x => x.Id == task.Id)!;
        //                clientTask = updatedDisplayTask;
        //                // flag for sync
        //                clientTask.ClientLastUpdated = DateTimeOffset.Now;
        //                // if had an update pending push, don't add a duplicate to changedTaskData
        //                var alreadyQueued = _dataState.ChangedTaskData.Where(
        //                    x => x.Id == clientTask.Id);
        //                if (alreadyQueued.Count() == 0) { _dataState.ChangedTaskData.Add(clientTask.Clone()); }
        //            }
        //            else
        //            {
        //                TaskDisplayModel unpushedClientTask = _dataState.GetTasks()!.Find
        //                    (x => x.TempLocalId == updatedDisplayTask.TempLocalId)!;
        //                unpushedClientTask = updatedDisplayTask;
        //                // flag for sync
        //                unpushedClientTask.ClientLastUpdated = DateTimeOffset.Now;
        //                // if had an update pending push, don't add a duplicate to changedTaskData
        //                var alreadyQueued = _dataState.ChangedTaskData.Where(
        //                    x => x.TempLocalId == unpushedClientTask.TempLocalId);
        //                if (alreadyQueued.Count() == 0) { _dataState.ChangedTaskData.Add(unpushedClientTask.Clone()); }
        //            }
        //        }
        //    }

        //    // trigger UI update
        //    _dataState.InvokeDataStateChanged("Tasks");
        //}
    }
}
