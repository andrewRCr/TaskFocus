using AutoMapper;
using System.Threading.Tasks;
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
                    await HandleTaskProjectChanged(task);
                }

                if (_dataHelper.HasTaskContextNameChanged(task))
                {
                    await HandleTaskContextChanged(task);
                }

                // if has both project and context, task is no longer in inbox
                if (task.ProjectId != null && task.ContextId != null) 
                {
                    //ShiftInboxSourceIndices(task);
                    ShiftCollectionSourceIndices(task, "InboxIndex");
                    task.InboxIndex = null; 
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

        public void ShiftCollectionSourceIndices(TaskModel task, string indexType)
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

                default:
                    break;
            }
        }

        //// task is being moved out of the inbox
        //public void ShiftInboxSourceIndices(TaskModel task)
        //{
        //    int? previouslyAssignedInboxIndex = task.InboxIndex;

        //    if (previouslyAssignedInboxIndex != null)
        //    {
        //        List<TaskDisplayModel> inboxTasks = _dataState.Tasks!
        //            .Where(x => x.ProjectId == null || x.ContextId == null).ToList();

        //        foreach (TaskDisplayModel inboxTask in inboxTasks)
        //        {
        //            // shift downwards
        //            if (inboxTask.InboxIndex > previouslyAssignedInboxIndex)
        //            {
        //                inboxTask.InboxIndex--;
        //            }
        //        }
        //    }
        //}

        //// task is being moved out of another existing project
        //public void ShiftProjectSourceIndices(TaskModel task)
        //{
        //    int? previouslyAssignedProjectId = null;
        //    int? previouslyAssignedProjectIndex = null;

        //    // if no contextId, task is still in inbox - so do nothing yet
        //    if (task.ProjectId != null && task.ContextId != null)
        //    {
        //        previouslyAssignedProjectId = task.ProjectId;
        //        previouslyAssignedProjectIndex = task.ProjectIndex;

        //        List<TaskDisplayModel> previousProjectTasks = _dataState.Tasks!
        //            .Where(x => x.ProjectId == previouslyAssignedProjectId).ToList();

        //        foreach (TaskDisplayModel previousNeighborTask in previousProjectTasks)
        //        {
        //            // shift downwards
        //            if (previousNeighborTask.ProjectIndex > previouslyAssignedProjectIndex)
        //            {
        //                previousNeighborTask.ProjectIndex--;
        //            }
        //        }
        //    }
        //}

        //// task is being moved out of another existing context
        //public void ShiftContextSourceIndices(TaskModel task)
        //{
        //    int? previouslyAssignedContextId = null;
        //    int? previouslyAssignedContextIndex = null;

        //    // if no projectId, task is still in inbox - so do nothing yet
        //    if (task.ContextId != null && task.ProjectId != null)
        //    {
        //        previouslyAssignedContextId = task.ContextId;
        //        previouslyAssignedContextIndex = task.ContextIndex;

        //        List<TaskDisplayModel> previousContextTasks = _dataState.Tasks!
        //            .Where(x => x.ContextId == previouslyAssignedContextId).ToList();

        //        foreach (TaskDisplayModel previousNeighborTask in previousContextTasks)
        //        {
        //            // shift downwards
        //            if (previousNeighborTask.ContextIndex > previouslyAssignedContextIndex)
        //            {
        //                previousNeighborTask.ContextIndex--;
        //            }
        //        }
        //    }
        //}

        public async Task HandleTaskProjectChanged(TaskModel task)
        {
            //ShiftProjectSourceIndices(task);
            ShiftCollectionSourceIndices(task, "ProjectIndex");

            if (task.ProjectName == null) // project was unassigned
            { 
                task.ProjectId = null;
                task.ProjectIndex = null;

                List<TaskDisplayModel> inboxTasks = _dataState.Tasks!
                    .Where(x => x.ProjectId == null || x.ContextId == null).ToList();

                task.InboxIndex = inboxTasks.Count > 0 ? inboxTasks.Count : 0;
                Console.WriteLine($"{task.TaskName}: new InboxIndex is {task.InboxIndex}");

            }
            else // has new assigned project
            {
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
                    task.ProjectIndex = 0;
                }
                else
                {
                    // determine project index for task
                    List<TaskDisplayModel> projectTasks = _dataState.Tasks!
                        .Where(x => x.ProjectId == assignedProject.Id).ToList();

                    task.ProjectIndex = projectTasks.Count > 0 ? projectTasks.Count : 0;
                    Console.WriteLine($"{task.TaskName}: new ProjectIndex is {task.ProjectIndex}");
                }

                task.ProjectId = assignedProject!.Id;
            }
        }

        public async Task HandleTaskContextChanged(TaskModel task)
        {
            //ShiftContextSourceIndices(task);
            ShiftCollectionSourceIndices(task, "ContextIndex");

            if (task.ContextName == null)  // context was unassigned
            { 
                task.ContextId = null;
                task.ContextIndex = null;

                List<TaskDisplayModel> inboxTasks = _dataState.Tasks!
                    .Where(x => x.ProjectId == null || x.ContextId == null).ToList();

                task.InboxIndex = inboxTasks.Count > 0 ? inboxTasks.Count : 0;
                Console.WriteLine($"{task.TaskName}: new InboxIndex is {task.InboxIndex}");
            }
            else // has new assigned context
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
                    task.ContextIndex = 0;
                }
                else
                {
                    // determine context index for task
                    List<TaskDisplayModel> contextTasks = _dataState.Tasks!
                        .Where(x => x.ContextId == assignedContext.Id).ToList();

                    task.ContextIndex = contextTasks.Count > 0 ? contextTasks.Count : 0;
                    Console.WriteLine($"{task.TaskName}: new ContextIndex is {task.ContextIndex}");
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
                Console.WriteLine($"{task.TaskName}: new InboxIndex is {task.InboxIndex}");
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
                ShiftCollectionSourceIndices(task, "InboxIndex");
            }
            if (task.ProjectId != null) 
            {
                //ShiftProjectSourceIndices(task);
                ShiftCollectionSourceIndices(task, "ProjectIndex");
            }
            if (task.ContextId != null) 
            {
                //ShiftContextSourceIndices(task);
                ShiftCollectionSourceIndices(task, "ContextIndex");
            }

            await _taskEndpoint.DeleteTask(task);

            // refresh all data
            await FetchRemoteTaskData();
            await FetchRemoteProjectData();
            await FetchRemoteContextData();
        }
    }
}
