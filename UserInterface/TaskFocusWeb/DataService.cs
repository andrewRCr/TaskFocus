using AutoMapper;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusWeb
{
    public class DataService : IDataService
    {
        private IAPIHelper _apiHelper;
        ITaskEndpoint _taskEndpoint;
        IProjectEndpoint _projectEndpoint;
        IContextEndpoint _contextEndpoint;
        IMapper _mapper;
        IDataHelper _dataHelper;
        DataState _dataState;

        public DataService(IAPIHelper apiHelper, ITaskEndpoint taskEndpoint, IProjectEndpoint projectEndpoint,
            IContextEndpoint contextEndpoint, IMapper mapper, IDataHelper dataHelper, DataState dataState)
        {
            _apiHelper = apiHelper;
            _taskEndpoint = taskEndpoint;
            _projectEndpoint = projectEndpoint;
            _contextEndpoint = contextEndpoint;
            _mapper = mapper;
            _dataHelper = dataHelper;
            _dataState = dataState;
        }

        public async Task FetchAllRemoteData()
        {
            Console.WriteLine("DataService: FetchAllRemoteData called");
            await FetchRemoteTaskData();
            await FetchRemoteProjectData();
            await FetchRemoteContextData();
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
            var displayProjectList = _mapper.Map<List<ProjectDisplayModel>>(projectList);
            _dataState.Projects = new List<ProjectDisplayModel>(displayProjectList);
        }

        public async Task FetchRemoteContextData()
        {
            var contextList = await _contextEndpoint.GetAllContextsForUser();
            var displayContextList = _mapper.Map<List<ContextDisplayModel>>(contextList);
            _dataState.Contexts = new List<ContextDisplayModel>(displayContextList);
        }

        // post updated task data to API for a single task
        public async Task UpdateTaskData(TaskDisplayModel displayTask)
        {
            // map from TaskDisplayModel to TaskModel
            TaskModel task = _mapper.Map<TaskModel>(displayTask);

            if (_dataHelper.HasTaskDataChanged(task))
            {
                if (_dataHelper.HasTaskProjectNameChanged(task))
                {
                    await AssignProjectIdFromProjectName(task);
                }

                if (_dataHelper.HasTaskContextNameChanged(task))
                {
                    await AssignContextIdFromContextName(task);
                }

                await _taskEndpoint.UpdateTask(task);

                // refresh Tasks + repopulate TasksLastFetch
                await FetchRemoteTaskData();
            }
        }

        // post updated project data to API for a single project
        public async Task UpdateProjectData(ProjectDisplayModel displayProject)
        {
            // map from ProjectDisplayModel to ProjectModel
            ProjectModel project = _mapper.Map<ProjectModel>(displayProject);

            if (true)
            {
                // if (DataHelper.HasTaskContextNameChanged(task))
                // {
                //     await AssignContextIdFromContextName(task);
                // }



                await _projectEndpoint.UpdateProject(project);

                // refresh data + repopulate TasksLastFetch
                await FetchRemoteTaskData();
                await FetchRemoteProjectData();
            }
        }

        // post updated context data to API for a single context
        public async Task UpdateContextData(ContextDisplayModel displayContext)
        {
            // map from ContextDisplayModel to ContextModel
            ContextModel context = _mapper.Map<ContextModel>(displayContext);

            if (true)
            {
                // if (DataHelper.HasTaskContextNameChanged(task))
                // {
                //     await AssignContextIdFromContextName(task);
                // }



                await _contextEndpoint.UpdateContext(context);

                // refresh data + repopulate TasksLastFetch
                await FetchAllRemoteData();
            }
        }

        public async Task AssignProjectIdFromProjectName(TaskModel task)
        {
            if (task.ProjectName == null) { task.ProjectId = null; }
            else
            {
                // lookup projectId by projectName and assign
                // TODO: need to enforce uniqueness of the projectName property - casing, etc; something. ensure these will match!

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
                }

                task.ProjectId = assignedProject!.Id;
            }
        }

        public async Task AssignContextIdFromContextName(TaskModel task)
        {
            if (task.ContextName == null) { task.ContextId = null; }
            else
            {
                // lookup ContextId by contextName and assign
                // TODO: need to enforce uniqueness of the contextName property - casing, etc; something. ensure these will match!

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
                }

                task.ContextId = assignedContext!.Id;
            }
        }

        public async Task AddProject(ProjectModel newProject)
        {
            if (string.IsNullOrWhiteSpace(newProject.ProjectName)) { return; }

            await _projectEndpoint.AddProject(newProject, _apiHelper.GetLoggedInUserId());

            // refresh Tasks, Projects, Contexts + clear NewTask
            await FetchRemoteTaskData();
            await FetchRemoteProjectData();
            await FetchRemoteContextData();
        }

        public async Task AddContext(ContextModel newContext)
        {
            if (string.IsNullOrWhiteSpace(newContext.ContextName)) { return; }

            await _contextEndpoint.AddContext(newContext, _apiHelper.GetLoggedInUserId());

            // refresh Tasks, Projects, Contexts + clear NewTask
            await FetchRemoteTaskData();
            await FetchRemoteProjectData();
            await FetchRemoteContextData();
        }
    }
}
