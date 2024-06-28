using AutoMapper;
using Caliburn.Micro;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TaskFocusDesktop.Commands;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.Base
{
    public abstract class TaskViewModelBase : ViewModelBase
    {
        IAPIHelper _apiHelper;
        IUserEndpoint _userEndpoint;
        ITaskEndpoint _taskEndpoint;
        IProjectEndpoint _projectEndpoint;
        IContextEndpoint _contextEndpoint;
        IMapper _mapper;
        IDataHelper _dataHelper;
        protected IWindowManager _window;
        public RelayCommand DeleteTaskCommand => new RelayCommand(async execute => await DeleteTask());

        public TaskViewModelBase(IAPIHelper apiHelper, IUserEndpoint userEndpoint, ITaskEndpoint taskEndpoint, IProjectEndpoint projectEndpoint,
            IContextEndpoint contextEndpoint, IMapper mapper, IDataHelper dataHelper, IWindowManager windowManager, IEventAggregator events) : base(events)
        {
            _apiHelper = apiHelper;
            _userEndpoint = userEndpoint;
            _taskEndpoint = taskEndpoint;
            _projectEndpoint = projectEndpoint;
            _contextEndpoint = contextEndpoint;
            _mapper = mapper;
            _dataHelper = dataHelper;
            _window = windowManager;
        }

        public List<TaskModel> TasksLastFetch { get; set; }

        private BindingList<TaskDisplayModel> _tasks;
        public BindingList<TaskDisplayModel> Tasks
        {
            get { return _tasks; }
            set
            {
                _tasks = value;
                NotifyOfPropertyChange(() => Tasks);
            }
        }

        private BindingList<TaskDisplayModel> _newTaskList;
        public BindingList<TaskDisplayModel> NewTaskList
        {
            get { return _newTaskList; }
            set
            {
                _newTaskList = value;
                NotifyOfPropertyChange(() => NewTaskList);
            }
        }

        private TaskDisplayModel _selectedTask;
        public TaskDisplayModel SelectedTask
        {
            get { return _selectedTask; }
            set
            {
                _selectedTask = value;
                NotifyOfPropertyChange(() => SelectedTask);
            }
        }

        private TaskDisplayModel _newTask;
        public TaskDisplayModel NewTask
        {
            get { return _newTask; }
            set
            {
                _newTask = value;
                NotifyOfPropertyChange(() => NewTask);
            }
        }

        private BindingList<ProjectModel> _projects;
        public BindingList<ProjectModel> Projects
        {
            get { return _projects; }
            set
            {
                _projects = value;
                NotifyOfPropertyChange(() => Projects);
            }
        }

        private BindingList<ContextModel> _contexts;
        public BindingList<ContextModel> Contexts
        {
            get { return _contexts; }
            set
            {
                _contexts = value;
                NotifyOfPropertyChange(() => Contexts);
            }
        }

        public bool CanAddTask
        {
            get
            {
                return NewTask != null ? !string.IsNullOrWhiteSpace(NewTask.TaskName) : false;
            }
        }

        protected override void OnViewLoaded(object view)
        {
            // nothing local, currently.
            // children will assign themselves as the ActiveViewModel
            // and then call LoadTasks()
        }

        protected async Task LoadTasks()
        {
            Task<List<TaskModel>> loadTaskToCall = null;

            //switch (ActiveViewModel)
            //{
            //    case ViewModelChildren.InboxVM:
            //        loadTaskToCall = _taskEndpoint.GetInboxTasksForUser();
            //        break;
            //    default:
            //        loadTaskToCall = _taskEndpoint.GetAllTasksForUser();
            //        break;
            //}

            //var taskList = await loadTaskToCall;
            //TasksLastFetch = taskList; // store for comparison

            //var displayTaskList = _mapper.Map<List<TaskDisplayModel>>(taskList);
            //Tasks = new BindingList<TaskDisplayModel>(displayTaskList);

            //foreach (TaskDisplayModel displayTask in Tasks)
            //{
            //    displayTask.PropertyChanged += OnExistingTaskPropertyChanged; // subscribe to property changed event
            //}

            //var projectList = await _projectEndpoint.GetAllProjectsForUser();
            //Projects = new BindingList<ProjectModel>(projectList);

            //var contextList = await _contextEndpoint.GetAllContextsForUser();
            //Contexts = new BindingList<ContextModel>(contextList);

            //// new task input placeholder
            //List<TaskDisplayModel> newTaskList = new List<TaskDisplayModel>();
            //TaskDisplayModel newTaskPlaceholder = new TaskDisplayModel();
            //NewTask = newTaskPlaceholder;
            //NewTask.PropertyChanged += OnNewTaskPropertyChanged; // subscribe to property changed event
            //newTaskList.Add(newTaskPlaceholder);
            //NewTaskList = new BindingList<TaskDisplayModel>(newTaskList);
        }

        public async Task AssignProjectIdFromProjectName(TaskModel task)
        {
            if (task.ProjectName != null)
            {
                // lookup projectId by projectName and assign
                // TODO: need to enforce uniqueness of the projectName property - casing, etc; something. ensure these will match!

                ProjectModel FindAssignedProject()
                {
                    List<ProjectModel> userProjects = Projects.ToList();
                    return userProjects.Find(x => x.ProjectName == task.ProjectName);
                }

                ProjectModel assignedProject = FindAssignedProject();
                if (assignedProject == null)
                {
                    ProjectModel newProject = new ProjectModel { ProjectName = task.ProjectName };
                    await AddProject(newProject);

                    assignedProject = FindAssignedProject();
                }

                task.ProjectId = assignedProject.Id;
            }
        }

        public async Task AssignContextIdFromContextName(TaskModel task)
        {
            if (task.ContextName != null)
            {
                // lookup ContextId by contextName and assign
                // TODO: need to enforce uniqueness of the contextName property - casing, etc; something. ensure these will match!

                ContextModel FindAssignedContext()
                {
                    List<ContextModel> userContexts = Contexts.ToList();
                    return userContexts.Find(x => x.ContextName == task.ContextName);
                }

                ContextModel assignedContext = FindAssignedContext();

                if (assignedContext == null)
                {
                    ContextModel newContext = new ContextModel { ContextName = task.ContextName };
                    await AddContext(newContext);

                    assignedContext = FindAssignedContext();
                }

                task.ContextId = assignedContext.Id;
            }
        }

        public async Task AddTask()
        {
            if (string.IsNullOrWhiteSpace(NewTask.TaskName)) { return; }

            // map from TaskDisplayModel to TaskModel
            TaskModel newTask = _mapper.Map<TaskModel>(NewTask);

            if (newTask.ProjectName != null)
            {
                await AssignProjectIdFromProjectName(newTask);
                // TODO: need to enforce uniqueness of the projectName property - casing, etc; something. ensure these will match!
            }

            if (newTask.ContextName != null)
            {
                await AssignContextIdFromContextName(newTask);
                // TODO: need to enforce uniqueness of the contextName property - casing, etc; something. ensure these will match!
            }

            await _taskEndpoint.AddTask(newTask, _apiHelper.GetLoggedInUserId());

            // refresh Tasks + clear NewTask
            await LoadTasks();
        }

        public async Task DeleteTask()
        {
            if (SelectedTask == null) { return; }

            // map from TaskDisplayModel to TaskModel
            TaskModel taskToDelete = _mapper.Map<TaskModel>(SelectedTask);

            await _taskEndpoint.DeleteTask(taskToDelete);

            // refresh Tasks + repopulate TasksLastFetch
            await LoadTasks();
        }

        // post updated task data to API for a single displayTask
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
                await LoadTasks();
            }
        }

        // saves updated task data to server on property change
        private async void OnExistingTaskPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string changedProperty = e.PropertyName;
            TaskDisplayModel senderTask = (TaskDisplayModel)sender;
            // map from TaskDisplayModel to TaskModel
            //TaskModel task = _mapper.Map<TaskModel>(senderTask);

            await UpdateTaskData(senderTask);
        }

        private void OnNewTaskPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string changedProperty = e.PropertyName;

            if (changedProperty == "TaskName")
            {
                NotifyOfPropertyChange(() => CanAddTask);
            }
        }

        public async Task AddProject(ProjectModel newProject)
        {
            if (string.IsNullOrWhiteSpace(newProject.ProjectName)) { return; }

            await _projectEndpoint.AddProject(newProject, _apiHelper.GetLoggedInUserId());

            // refresh Tasks, Projects, Contexts + clear NewTask
            await LoadTasks();
        }

        public async Task AddContext(ContextModel newContext)
        {
            if (string.IsNullOrWhiteSpace(newContext.ContextName)) { return; }

            await _contextEndpoint.AddContext(newContext, _apiHelper.GetLoggedInUserId());

            // refresh Tasks, Projects, Contexts + clear NewTask
            await LoadTasks();
        }
    }
}
