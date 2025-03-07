using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Utilities
{
    public class DataService : IDataService
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

        private int _taskUpdateEntered = 0;
        private int _projectUpdateEntered = 0;
        private int _contextUpdateEntered = 0;
        private int _settingsUpdateEntered = 0;
        private int _userUpdateEntered = 0;

        TaskDisplayModel? _taskBeingUpdated;
        ProjectModel? _projectBeingUpdated;
        ContextModel? _contextBeingUpdated;

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

        // wrapper for info logging when used in desktop UI w/ caliburn micro
        private void LogInformation(string message)
        {
            if (_logger != null) { _logger.LogInformation(message);}
            else { Debug.WriteLine($"DesktopUI - INFO: {message}");}
        }

        // wrapper for error logging when used in desktop UI w/ caliburn micro
        private void LogError(string message)
        {
            if (_logger != null) { _logger.LogError(message); }
            else { Debug.WriteLine($"DesktopUI - ERROR: {message}"); }
        }

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

        public virtual async Task FetchRemoteTaskData()
        {
            var taskList = await _taskEndpoint.GetAllTasksForUser();
            _dataHelper.TasksLastFetch = taskList; // store for comparison

            var displayTaskList = _mapper.Map<List<TaskDisplayModel>>(taskList);
            _dataState.Tasks = new List<TaskDisplayModel>(displayTaskList);
        }

        public async Task FetchRemoteProjectData()
        {
            var projectList = await _projectEndpoint.GetAllProjectsForUser();
            _dataHelper.ProjectsLastFetch = projectList; // store for comparison

            var displayProjectList = _mapper.Map<List<ProjectDisplayModel>>(projectList);
            _dataState.Projects = new List<ProjectDisplayModel>(displayProjectList);
        }

        public async Task FetchRemoteProjectAndTasksById(int id)
        {
            var project = await _projectEndpoint.GetProjectById(id);
            var displayProject = _mapper.Map<ProjectDisplayModel>(project);
            _dataHelper.FocusedProject = displayProject;

            var projectTasks = await _taskEndpoint.GetAllProjectTasksById(id);
            var displayProjectTasks = _mapper.Map<List<TaskDisplayModel>>(projectTasks);
            _dataHelper.FocusedProjectTasks = displayProjectTasks;
        }

        public async Task FetchRemoteContextData()
        {
            var contextList = await _contextEndpoint.GetAllContextsForUser();
            _dataHelper.ContextsLastFetch = contextList; // store for comparison

            var displayContextList = _mapper.Map<List<ContextDisplayModel>>(contextList);
            _dataState.Contexts = new List<ContextDisplayModel>(displayContextList);
        }

        public async Task FetchRemoteContextAndTasksById(int id)
        {
            var context = await _contextEndpoint.GetContextById(id);
            var displayContext = _mapper.Map<ContextDisplayModel>(context);
            _dataHelper.FocusedContext = displayContext;

            var contextTasks = await _taskEndpoint.GetAllContextTasksById(id);
            var displayContextTasks = _mapper.Map<List<TaskDisplayModel>>(contextTasks);
            _dataHelper.FocusedContextTasks = displayContextTasks;
        }

        public async Task FetchRemoteSettingsData()
        {
            var userSettings = await _userEndpoint.GetCurrentUserSettings();
            _dataHelper.UserSettingsLastFetch = userSettings; // store for comparison

            var displayUserSettings = _mapper.Map<UserSettingsDisplayModel>(userSettings);
            _dataState.UserSettings = displayUserSettings;
        }

        public async Task FetchRemoteUserData()
        {
            var userData = await _userEndpoint.GetCurrentUserData();
            // store for comparison?

            var displayUserData = _mapper.Map<UserDisplayModel>(userData);
            _dataState.CurrentUser = displayUserData;
        }

        public TaskModel MapToRawTask(TaskDisplayModel displayTask)
        {
            return _mapper.Map<TaskModel>(displayTask);
        }

        public async Task AddTask(TaskDisplayModel displayTask)
        {
            // map from TaskDisplayModel to TaskModel
            TaskModel task = _mapper.Map<TaskModel>(displayTask);

            if (string.IsNullOrWhiteSpace(task.TaskName)) { return; }

            if (task.ProjectName == null || task.ContextName == null)
            {
                List<TaskDisplayModel> inboxTasks = _dataState.Tasks!
                    .Where(x => x.ProjectId == null || x.ContextId == null).ToList();

                task.InboxIndex = inboxTasks.Count > 0 ? inboxTasks.Count : 0;
                //LogInformation($"{task.TaskName}: new InboxIndex is {task.InboxIndex}");
            }

            if (task.ProjectName != null)
            {
                await HandleTaskProjectChanged(task);
            }
            if (task.ContextName != null)
            {
                await HandleTaskContextChanged(task);
            }

            // * INSTEAD OF THIS... *
            //await _taskEndpoint.AddTask(task, _apiHelper.GetLoggedInUserId());
            //// refresh all data
            //await FetchRemoteTaskData();
            //await FetchRemoteProjectData();
            //await FetchRemoteContextData();

            // * ONLY ADD LOCALLY AND MARK FOR SYNC *
            // update local datastate
            TaskDisplayModel updatedDisplayTask = _mapper.Map<TaskDisplayModel>(task);
            // flag for sync
            updatedDisplayTask.ClientLastUpdated = DateTimeOffset.Now;
            // give temp local tracking id
            updatedDisplayTask.TempLocalId = ++_dataState.TempTaskId;
            _dataState.ChangedTaskData.Add(updatedDisplayTask);
            _dataState.Tasks!.Add(updatedDisplayTask);
            // trigger UI update
            _dataState.InvokeDataStateChanged("Tasks");
        }

        public async Task DeleteTask(TaskDisplayModel displayTask)
        {
            // map from TaskDisplayModel to TaskModel
            TaskModel task = _mapper.Map<TaskModel>(displayTask);
            HandleIndexShiftsOnTaskDeletion(task);

            // * INSTEAD OF THIS... *
            //await _taskEndpoint.DeleteTask(task);
            // refresh all data
            //await FetchRemoteTaskData();
            //await FetchRemoteProjectData();
            //await FetchRemoteContextData();

            // * ONLY MARK FOR DELETE ON NEXT SYNC + DELETE LOCALLY *
            // update local datastate

            if (task.Id == null) // never existed on server; insert was pending push
            {
                // local delete
                _dataState.Tasks!.Remove(displayTask);
                var changedTask = _dataState.ChangedTaskData.Find(x => x.TempLocalId == displayTask.TempLocalId);
                bool removed = _dataState.ChangedTaskData.Remove(changedTask!);
                // trigger UI update
                _dataState.InvokeDataStateChanged("Tasks");

                if (removed)
                    Console.WriteLine("never existed on server; removed from changed task data");
                else
                    Console.WriteLine("never existed on server; COULDN'T FIND IN CHANGEDTASKDATA TO REMOVE!");
            }
            else
            {
                TaskDisplayModel clientTask = _dataState.Tasks!.Find(x => x.Id == task.Id)!;
                clientTask.Deleted = DateTimeOffset.Now;
                // flag for sync
                clientTask.ClientLastUpdated = DateTimeOffset.Now;

                // if had an update pending push, don't add a duplicate to changedTaskData
                var alreadyQueued = _dataState.ChangedTaskData.Where(
                    x => x.Id == clientTask.Id);
                if (alreadyQueued.Count() == 0) { _dataState.ChangedTaskData.Add(clientTask); }

                // local delete
                _dataState.Tasks!.Remove(clientTask);
                // trigger UI update
                _dataState.InvokeDataStateChanged("Tasks");
            }
        }

        public void HandleIndexShiftsOnTaskDeletion(TaskModel task)
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


        // process updated task data locally + flag for sync
        public async Task UpdateTaskData(TaskDisplayModel displayTask, bool forceUpdate = false)
        {
            TaskModel task = _mapper.Map<TaskModel>(displayTask);
            TaskDataCompareResult compareResult = _dataHelper.HasTaskDataChanged(displayTask);

            if (compareResult.HasChanged || forceUpdate)
            {
                if (_taskBeingUpdated != null)
                {
                    if (task.Id == null && displayTask.TempLocalId != _taskBeingUpdated.Id ||
                        task.Id != _taskBeingUpdated.Id)
                    {
                        // unlock
                        Interlocked.Exchange(ref _taskUpdateEntered, 0);
                        _taskBeingUpdated = null;
                    }
                }

                // lock
                if (Interlocked.Increment(ref _taskUpdateEntered) != 1) { return; }
                _taskBeingUpdated = displayTask;

                // PROCESS UPDATE
                // handle change of collection task belongs to
                if (compareResult.ProjectNameChanged) { await HandleTaskProjectChanged(task); }
                if (compareResult.ContextNameChanged) { await HandleTaskContextChanged(task); }
                // handle any adjustments to which view pages the task appears in
                HandleTaskViewChanges(task);

                // change locally and mark for sync
                TaskDisplayModel updatedDisplayTask = _mapper.Map<TaskDisplayModel>(task);
                if (displayTask.Id == null)
                {
                    // task hasn't yet been inserted on server; pending push
                    updatedDisplayTask.ClientLastUpdated = DateTimeOffset.Now;

                    var queuedNewTask = _dataState.ChangedTaskData.Where(
                        x => x.TempLocalId == displayTask.TempLocalId).FirstOrDefault();

                    queuedNewTask = updatedDisplayTask; // modify
                }
                else
                {
                    TaskDisplayModel clientTask = _dataState.Tasks!.Find(x => x.Id == task.Id)!;
                    clientTask = updatedDisplayTask; // modify
                    clientTask.ClientLastUpdated = DateTimeOffset.Now; // flag for sync

                    // don't duplicate if already had another update pending prior to push
                    var alreadyQueued = _dataState.ChangedTaskData.Where(
                        x => x.Id == clientTask.Id);
                    if (!alreadyQueued.Any()) {  _dataState.ChangedTaskData.Add(clientTask); }
                }

                // unlock
                Interlocked.Exchange(ref _taskUpdateEntered, 0);
                _taskBeingUpdated = null;
            }
        }

        public void HandleTaskViewChanges(TaskModel task)
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
                List<TaskDisplayModel> starredTasks = _dataState.Tasks!
                    .Where(x => x.Starred).ToList();

                List<TaskDisplayModel> dueTasks = _dataState.Tasks!
                    .Where(x => x.DueDate <= DateTime.Now.Date).ToList();

                List<TaskDisplayModel> todayTasks = dueTasks.Concat(starredTasks).ToList();
                todayTasks = todayTasks.DistinctBy(x => x.Id).ToList();

                task.TodayIndex = todayTasks.Count > 1 ? (todayTasks.Count - 1) : 0;
            }
        }

        public async Task HandleTaskProjectChanged(TaskModel task)
        {
            ShiftTaskCollectionSourceIndices(task, "ProjectIndex");

            if (task.ProjectName == null) // project was unassigned
            {
                task.ProjectId = null;
                task.ProjectIndex = null;

                // if not already in inbox, will need InboxIndex assigned
                if (task.InboxIndex == null)
                {
                    List<TaskDisplayModel> inboxTasks = _dataState.Tasks!
                        .Where(x => x.ProjectId == null || x.ContextId == null).ToList();

                    task.InboxIndex = inboxTasks.Count > 0 ? inboxTasks.Count : 0;
                    //LogInformation($"{task.TaskName}: new InboxIndex is {task.InboxIndex}");
                }
            }
            else // has new assigned project
            {
                // lookup ProjectId by projectName and assign
                // note: unique project names are enforced on add/update
                ProjectDisplayModel? FindAssignedProject()
                {
                    List<ProjectDisplayModel> userProjects = _dataState.Projects!.ToList();
                    return userProjects.Find(x => x.ProjectName == task.ProjectName);
                }

                ProjectDisplayModel? assignedProject = FindAssignedProject();
                if (assignedProject == null)
                {
                    ProjectModel newProject = new ProjectModel { ProjectName = task.ProjectName };
                    await AddProject(newProject);

                    assignedProject = FindAssignedProject();
                    task.ProjectIndex = 0;
                }
                else
                {
                    // determine project index for task
                    List<TaskDisplayModel> projectTasks = _dataState.Tasks!
                        .Where(x => x.ProjectId == assignedProject.Id).ToList();

                    task.ProjectIndex = projectTasks.Count > 0 ? projectTasks.Count : 0;
                    //LogInformation($"{task.TaskName}: new ProjectIndex is {task.ProjectIndex}");
                }

                task.ProjectId = assignedProject!.Id;
            }
        }

        public async Task HandleTaskContextChanged(TaskModel task)
        {
            ShiftTaskCollectionSourceIndices(task, "ContextIndex");

            if (task.ContextName == null)  // context was unassigned
            {
                task.ContextId = null;
                task.ContextIndex = null;

                // if not already in inbox, will need InboxIndex assigned
                if (task.InboxIndex == null)
                {
                    List<TaskDisplayModel> inboxTasks = _dataState.Tasks!
                        .Where(x => x.ProjectId == null || x.ContextId == null).ToList();

                    task.InboxIndex = inboxTasks.Count > 0 ? inboxTasks.Count : 0;
                    //LogInformation($"{task.TaskName}: new InboxIndex is {task.InboxIndex}");
                }
            }
            else // has new assigned context
            {
                // lookup ContextId by contextName and assign
                // note: unique context names are enforced on add/update
                ContextDisplayModel? FindAssignedContext()
                {
                    List<ContextDisplayModel> userContexts = _dataState.Contexts!.ToList();
                    return userContexts.Find(x => x.ContextName == task.ContextName);
                }

                ContextDisplayModel? assignedContext = FindAssignedContext();
                if (assignedContext == null)
                {
                    ContextModel newContext = new ContextModel { ContextName = task.ContextName };
                    await AddContext(newContext);

                    assignedContext = FindAssignedContext();
                    task.ContextIndex = 0;
                }
                else
                {
                    // determine context index for task
                    List<TaskDisplayModel> contextTasks = _dataState.Tasks!
                        .Where(x => x.ContextId == assignedContext.Id).ToList();

                    task.ContextIndex = contextTasks.Count > 0 ? contextTasks.Count : 0;
                    //LogInformation($"{task.TaskName}: new ContextIndex is {task.ContextIndex}");
                }

                task.ContextId = assignedContext!.Id;
            }
        }

        // for use when removing a task *from* the inbox or a project/context
        public void ShiftTaskCollectionSourceIndices(TaskModel task, string indexType)
        {
            int? previouslyAssignedCollectionIndex = null;
            int? previouslyAssignedCollectionId = null;
            List<TaskDisplayModel>? previousCollectionTasks = null;

            switch (indexType)
            {
                case "InboxIndex": // task is being moved out of the inbox
                    previouslyAssignedCollectionIndex = task.InboxIndex;
                    previousCollectionTasks = _dataState.Tasks!
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
                        previousCollectionTasks = _dataState.Tasks!
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
                        previousCollectionTasks = _dataState.Tasks!
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
                        List<TaskDisplayModel> dueTasks = _dataState.Tasks!
                            .Where(x => x.DueDate <= DateTime.Now.Date).ToList();
                        List<TaskDisplayModel> starredTasks = _dataState.Tasks!
                            .Where(x => x.Starred).ToList();
                        previousCollectionTasks = dueTasks.Concat(starredTasks).ToList();
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

        // alternate update method - updates entire task collection prior to remote fetch
        public async Task UpdateCollectionOrderingIndices(List<TaskDisplayModel> displayTasks)
        {
            foreach (TaskDisplayModel displayTask in displayTasks)
            {
                TaskModel task = _mapper.Map<TaskModel>(displayTask);
                TaskDataCompareResult compareResult= _dataHelper.HasTaskDataChanged(displayTask);
                // only update if changed
                if (compareResult.HasChanged)
                {
                    // * INSTEAD OF THIS... *
                    //await _taskEndpoint.UpdateTask(task);

                    // * ONLY CHANGE LOCALLY AND MARK FOR SYNC *
                    // update local datastate
                    TaskDisplayModel updatedDisplayTask = _mapper.Map<TaskDisplayModel>(task);
                    TaskDisplayModel clientTask = _dataState.Tasks!.Find(x => x.Id == task.Id)!;
                    clientTask = updatedDisplayTask;
                    // flag for sync
                    clientTask.ClientLastUpdated = DateTimeOffset.Now;
                    _dataState.ChangedTaskData.Add(clientTask);
                }
            }

            //await FetchAllRemoteData();
        }

        // updates entire project collection prior to remote fetch
        public async Task UpdateProjectsOrderingIndices(List<ProjectDisplayModel> displayProjects)
        {
            foreach (ProjectDisplayModel displayProject in displayProjects)
            {
                // map from ProjectDisplayModel to ProjectModel
                ProjectModel project = _mapper.Map<ProjectModel>(displayProject);

                // only update if changed
                if (_dataHelper.HasProjectDataChanged(project))
                { 
                    await _projectEndpoint.UpdateProject(project); 
                }

            }

            await FetchAllRemoteData();
        }

        // updates entire context collection prior to remote fetch
        public async Task UpdateContextsOrderingIndices(List<ContextDisplayModel> displayContexts)
        {
            foreach (ContextDisplayModel displayContext in displayContexts)
            {
                // map from ContextDisplayModel to ContextModel
                ContextModel context = _mapper.Map<ContextModel>(displayContext);

                // only update if changed
                if (_dataHelper.HasContextDataChanged(context))
                {
                    await _contextEndpoint.UpdateContext(context);
                }
            }

            await FetchAllRemoteData();
        }

        // for use when removing a project/context
        public void ShiftCollectionOrderIndices<T>(T collectionDisplayModel, List<T> collectionSource) where T : ICollectionDisplayModel
        {
            int? previouslyAssignedCollectionIndex = collectionDisplayModel.OrderIndex;

            foreach (T item in collectionSource)
            {
                bool shiftNeeded = item.OrderIndex > previouslyAssignedCollectionIndex;
                if (shiftNeeded) { item.OrderIndex--; }
            }
        }

        public async Task AddProject(ProjectModel newProject)
        {
            if (string.IsNullOrWhiteSpace(newProject.ProjectName)) { return; }

            if (!_dataHelper.IsNewProjectNameUnique(newProject.ProjectName))
            {
                LogError("Unable to create project: project names must be unique.");
                return;
            }

            // determine OrderIndex for project
            newProject.OrderIndex = _dataState.Projects!.Count > 0 ? _dataState.Projects.Count : 0;

            await _projectEndpoint.AddProject(newProject, _apiHelper.GetLoggedInUserId());

            // refresh Tasks, Projects, Contexts + clear NewTask
            await FetchRemoteTaskData();
            await FetchRemoteProjectData();
            await FetchRemoteContextData();
        }

        public async Task DeleteProject(ProjectDisplayModel displayProject)
        {
            // map from ProjectDisplayModel to ProjectModel
            ProjectModel project = _mapper.Map<ProjectModel>(displayProject);

            // ensure other projects have updated OrderIndex values
            ShiftCollectionOrderIndices(displayProject, _dataState.Projects!);

            // handle project's tasks - remove assigned project
            List<TaskDisplayModel> projectTasks = _dataState.Tasks!.Where(x => x.ProjectId == project.Id).ToList();
            foreach (TaskDisplayModel task in projectTasks)
            {
                task.ProjectName = null;
                await UpdateTaskData(task);
            }

            await _projectEndpoint.DeleteProject(project);

            // refresh all data
            await FetchRemoteTaskData();
            await FetchRemoteProjectData();
            await FetchRemoteContextData();
        }

        // post updated project data to API for a single project
        public async Task UpdateProjectData(ProjectDisplayModel displayProject)
        {
            // map from ProjectDisplayModel to ProjectModel
            ProjectModel project = _mapper.Map<ProjectModel>(displayProject);

            if (_dataHelper.HasProjectDataChanged(project))
            {
                if (!_dataHelper.IsUpdatedProjectNameUnique(project))
                {
                    LogError("Unable to update project: project names must be unique.");
                    return;
                }

                if (_projectBeingUpdated != null && project.Id != _projectBeingUpdated.Id)
                {
                    // unlock
                    Interlocked.Exchange(ref _projectUpdateEntered, 0);
                    _projectBeingUpdated = null;
                }

                // lock
                if (Interlocked.Increment(ref _projectUpdateEntered) != 1) { return; }
                _projectBeingUpdated = project;

                // update + refresh
                await _projectEndpoint.UpdateProject(project);
                await FetchAllRemoteData();

                // unlock
                Interlocked.Exchange(ref _projectUpdateEntered, 0);
                _projectBeingUpdated = null;
            }
        }

        public async Task AddContext(ContextModel newContext)
        {
            if (string.IsNullOrWhiteSpace(newContext.ContextName)) { return; }

            if (!_dataHelper.IsNewContextNameUnique(newContext.ContextName))
            {
                LogError("Unable to create context: context names must be unique.");
                return;
            }

            // determine OrderIndex for context
            newContext.OrderIndex = _dataState.Contexts!.Count > 0 ? _dataState.Contexts.Count : 0;

            await _contextEndpoint.AddContext(newContext, _apiHelper.GetLoggedInUserId());

            // refresh Tasks, Projects, Contexts + clear NewTask
            await FetchRemoteTaskData();
            await FetchRemoteProjectData();
            await FetchRemoteContextData();
        }

        public async Task DeleteContext(ContextDisplayModel displayContext)
        {
            // map from ContextDisplayModel to ContextModel
            ContextModel context = _mapper.Map<ContextModel>(displayContext);

            // ensure other contexts have updated OrderIndex values
            ShiftCollectionOrderIndices(displayContext, _dataState.Contexts!);

            // handle context's tasks - remove assigned context
            List<TaskDisplayModel> contextTasks = _dataState.Tasks!.Where(x => x.ContextId == context.Id).ToList();
            foreach (TaskDisplayModel task in contextTasks)
            {
                task.ContextName = null;
                await UpdateTaskData(task);
            }

            await _contextEndpoint.DeleteContext(context);

            // refresh all data
            await FetchRemoteTaskData();
            await FetchRemoteProjectData();
            await FetchRemoteContextData();
        }

        // post updated context data to API for a single context
        public async Task UpdateContextData(ContextDisplayModel displayContext)
        {
            // map from ContextDisplayModel to ContextModel
            ContextModel context = _mapper.Map<ContextModel>(displayContext);

            if (_dataHelper.HasContextDataChanged(context))
            {
                if (!_dataHelper.IsUpdatedContextNameUnique(context))
                {
                    LogError("Unable to update context: context names must be unique.");
                    return;
                }

                if (_contextBeingUpdated != null && context.Id != _contextBeingUpdated.Id)
                {
                    // unlock
                    Interlocked.Exchange(ref _contextUpdateEntered, 0);
                    _contextBeingUpdated = null;
                }

                // lock
                if (Interlocked.Increment(ref _contextUpdateEntered) != 1) { return; }
                _contextBeingUpdated = context;

                // update + refresh
                await _contextEndpoint.UpdateContext(context);
                await FetchAllRemoteData();

                // unlock
                Interlocked.Exchange(ref _contextUpdateEntered, 0);
                _contextBeingUpdated = null;
            }
        }

        // update local datastate: user name data
        public async Task UpdateUserNameData(UserDisplayModel displayUserModel)
        {
            if (_dataState.IsDataLoaded())
            {
                // re-map
                UserModel user = _mapper.Map<UserModel>(displayUserModel);

                //if... (check if changed from last fetch?)

                // lock
                if (Interlocked.Increment(ref _userUpdateEntered) != 1) { return; }

                // * INSTEAD OF THIS... *
                //await _userEndpoint.UpdateName(user);
                //await FetchAllRemoteData();

                // * ONLY CHANGE LOCALLY AND MARK FOR SYNC *
                // update local datastate
                _dataState.CurrentUser!.FirstName = user.FirstName;
                _dataState.CurrentUser.LastName = user.LastName;

                // flag for sync
                _dataState.CurrentUser.ClientLastUpdated = DateTimeOffset.Now;
                _dataState.ChangedUserData.Add(_mapper.Map<UserModel>(_dataState.CurrentUser));

                // unlock
                Interlocked.Exchange(ref _userUpdateEntered, 0);

                Console.WriteLine("local user name updated!");
            }
        }

        public async Task RequestUpdateEmail(UserModel user)
        {
            //if... (check if changed from last fetch?)

            // lock
            if (Interlocked.Increment(ref _userUpdateEntered) != 1) { return; }

            await _userEndpoint.RequestUpdateEmail(user);
            await FetchAllRemoteData();

            // unlock
            Interlocked.Exchange(ref _userUpdateEntered, 0);
        }

        public async Task<bool> CheckUserExists(UserModel user)
        {
            bool exists = await _userEndpoint.CheckUserExists(user);
            return exists;
        }

        public async Task UpdatePassword(CreateUserModel updatedUserModel)
        {
            //if... (check if changed from last fetch?)

            // lock
            if (Interlocked.Increment(ref _userUpdateEntered) != 1) { return; }

            await _userEndpoint.UpdatePassword(updatedUserModel);

            // unlock
            Interlocked.Exchange(ref _userUpdateEntered, 0);
        }

        // updated user settings data to API
        public async Task UpdateSettingsData(UserSettingsDisplayModel displaySettings)
        {
            // re-map
            UserSettingsModel settings = _mapper.Map<UserSettingsModel>(displaySettings);

            if (_dataHelper.HasSettingsDataChanged(settings))
            {
                // lock
                if (Interlocked.Increment(ref _settingsUpdateEntered) != 1) { return; }

                await _userEndpoint.UpdateUserSettings(settings);
                await FetchAllRemoteData();

                // unlock
                Interlocked.Exchange(ref _settingsUpdateEntered, 0);
            }
        }
    }
}
