using AutoMapper;
using Microsoft.Extensions.Logging;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusWeb
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
        private DataState _dataState;

        private int _taskUpdateEntered = 0;
        private int _projectUpdateEntered = 0;
        private int _contextUpdateEntered = 0;
        private int _settingsUpdateEntered = 0;

        public DataService(IAPIHelper apiHelper, ILogger<DataService> logger, ITaskEndpoint taskEndpoint, IProjectEndpoint projectEndpoint,
            IContextEndpoint contextEndpoint, IUserEndpoint userEndpoint, IMapper mapper, IDataHelper dataHelper, DataState dataState)
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

        public async Task FetchAllRemoteData()
        {
            try
            {
                await FetchRemoteUserData();
                await FetchRemoteSettingsData();
                await FetchRemoteTaskData();
                await FetchRemoteProjectData();
                await FetchRemoteContextData();
                _logger.LogInformation("FetchAllRemoteData call processed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
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

        public async Task FetchRemoteContextData()
        {
            var contextList = await _contextEndpoint.GetAllContextsForUser();
            _dataHelper.ContextsLastFetch = contextList; // store for comparison

            var displayContextList = _mapper.Map<List<ContextDisplayModel>>(contextList);
            _dataState.Contexts = new List<ContextDisplayModel>(displayContextList);

            _logger.LogInformation($"Length of _dataState.Contexts after fetch call: {_dataState.Contexts.Count()}");

            foreach (ContextDisplayModel context in _dataState.Contexts)
            {
                _logger.LogInformation(context.ContextName);
            }

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
            _dataState.CurrentUser = userData;
        }

        public TaskModel MapToRawTask(TaskDisplayModel displayTask)
        {
            return _mapper.Map<TaskModel>(displayTask);
        }

        // post updated task data to API for a single task
        public async Task UpdateTaskData(TaskDisplayModel displayTask, bool forceUpdate = false)
        {
            // map from TaskDisplayModel to TaskModel
            TaskModel task = _mapper.Map<TaskModel>(displayTask);

            if (_dataHelper.HasTaskDataChanged(task) || forceUpdate)
            {
                // lock
                if (Interlocked.Increment(ref _taskUpdateEntered) != 1) { return; }

                if (_dataHelper.HasTaskProjectNameChanged(task))
                {
                    await HandleTaskProjectChanged(task);
                }

                if (_dataHelper.HasTaskContextNameChanged(task))
                {
                    await HandleTaskContextChanged(task);
                }

                // if has both project and context, task is no longer in inbox
                if (task.InboxIndex != null && ((task.ProjectId != null && task.ContextId != null) || (task.CleanedUp))) 
                {
                    ShiftTaskCollectionSourceIndices(task, "InboxIndex");
                    task.InboxIndex = null; 
                }

                // no longer in Today view
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

                // should be in Today view
                if (task.TodayIndex == null && (task.Starred || _dataHelper.IsTaskDueOrOverDue(task)) &&
                    !(task.Completed && task.DateCompleted != DateTime.Now.Date))
                {
                    //Console.WriteLine($"passed TodayIndex == null check! TodayIndex value: {task.TodayIndex}");

                    List<TaskDisplayModel> starredTasks = _dataState.Tasks!
                        .Where(x => x.Starred).ToList();

                    List<TaskDisplayModel> dueTasks = _dataState.Tasks!
                        .Where(x => x.DueDate <= DateTime.Now.Date).ToList();

                    List<TaskDisplayModel> todayTasks = dueTasks.Concat(starredTasks).ToList();
                    todayTasks = todayTasks.DistinctBy(x => x.Id).ToList();

                    task.TodayIndex = todayTasks.Count > 1 ? (todayTasks.Count - 1) : 0;
                    //Console.WriteLine($"{task.TaskName}: new TodayIndex is {task.TodayIndex}");
                }

                await _taskEndpoint.UpdateTask(task);
                await FetchAllRemoteData();

                // unlock
                Interlocked.Exchange(ref _taskUpdateEntered, 0);
            }
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
                    _logger.LogError("Unable to update project: project names must be unique.");
                    return; 
                }

                // lock
                if (Interlocked.Increment(ref _projectUpdateEntered) != 1) { return; }

                await _projectEndpoint.UpdateProject(project);
                await FetchAllRemoteData();

                // unlock
                Interlocked.Exchange(ref _projectUpdateEntered, 0);
            }
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
                    _logger.LogError("Unable to update context: context names must be unique.");
                    return;
                }

                // lock
                if (Interlocked.Increment(ref _contextUpdateEntered) != 1) { return; }

                await _contextEndpoint.UpdateContext(context);
                await FetchAllRemoteData();

                // unlock
                Interlocked.Exchange(ref _contextUpdateEntered, 0);
            }
        }

        //
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

        // for use when removing a project/context
        public void ShiftCollectionOrderIndices<T>(T collectionDisplayModel, List<T> collectionSource) where T: ICollectionDisplayModel
        {
            int? previouslyAssignedCollectionIndex = collectionDisplayModel.OrderIndex;

            foreach (T item in collectionSource)
            {
                bool shiftNeeded = item.OrderIndex > previouslyAssignedCollectionIndex;
                if (shiftNeeded) { item.OrderIndex--; }
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
                    //Console.WriteLine($"{task.TaskName}: new InboxIndex is {task.InboxIndex}");
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
                    //Console.WriteLine($"{task.TaskName}: new ProjectIndex is {task.ProjectIndex}");
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
                    //Console.WriteLine($"{task.TaskName}: new InboxIndex is {task.InboxIndex}");
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
                    //Console.WriteLine($"{task.TaskName}: new ContextIndex is {task.ContextIndex}");
                }

                task.ContextId = assignedContext!.Id;
            }
        }

        public async Task AddProject(ProjectModel newProject)
        {
            if (string.IsNullOrWhiteSpace(newProject.ProjectName)) { return; }

            if (!_dataHelper.IsNewProjectNameUnique(newProject.ProjectName))
            {
                _logger.LogError("Unable to create project: project names must be unique.");
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

        public async Task AddContext(ContextModel newContext)
        {
            if (string.IsNullOrWhiteSpace(newContext.ContextName)) { return; }

            if (!_dataHelper.IsNewContextNameUnique(newContext.ContextName))
            {
                _logger.LogError("Unable to create context: context names must be unique.");
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
                //Console.WriteLine($"{task.TaskName}: new InboxIndex is {task.InboxIndex}");
            }

            if (task.ProjectName != null)
            {
                await HandleTaskProjectChanged(task);
            }
            if (task.ContextName != null)
            {
                await HandleTaskContextChanged(task);
            }

            await _taskEndpoint.AddTask(task, _apiHelper.GetLoggedInUserId());

            // refresh all data
            await FetchRemoteTaskData();
            await FetchRemoteProjectData();
            await FetchRemoteContextData();
        }

        public async Task DeleteTask(TaskDisplayModel displayTask)
        {
            // map from TaskDisplayModel to TaskModel
            TaskModel task = _mapper.Map<TaskModel>(displayTask);

            if (task.ProjectId == null || task.ContextId == null) 
            { 
                //ShiftInboxSourceIndices(task);
                ShiftTaskCollectionSourceIndices(task, "InboxIndex");
            }
            if (task.ProjectId != null) 
            {
                //ShiftProjectSourceIndices(task);
                ShiftTaskCollectionSourceIndices(task, "ProjectIndex");
            }
            if (task.ContextId != null) 
            {
                //ShiftContextSourceIndices(task);
                ShiftTaskCollectionSourceIndices(task, "ContextIndex");
            }
            if (task.Starred)
            {
                ShiftTaskCollectionSourceIndices(task, "TodayIndex");
            }

            await _taskEndpoint.DeleteTask(task);

            // refresh all data
            await FetchRemoteTaskData();
            await FetchRemoteProjectData();
            await FetchRemoteContextData();
        }
    }
}
