using AutoMapper;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskFocusDesktop.Library.API;
using TaskFocusDesktop.Library.Models;
using TaskFocusDesktop.Models;

namespace TaskFocusDesktop.ViewModels
{
    public abstract class TaskViewModelBase : ViewModelBase
    {
        IUserEndpoint _userEndpoint;
        ITaskEndpoint _taskEndpoint;
        IProjectEndpoint _projectEndpoint;
        IMapper _mapper;
        protected IWindowManager _window;
     
        public TaskViewModelBase(IUserEndpoint userEndpoint, ITaskEndpoint taskEndpoint, IProjectEndpoint projectEndpoint,
            IMapper mapper, IWindowManager windowManager)
        {
            _userEndpoint = userEndpoint;
            _taskEndpoint = taskEndpoint;
            _projectEndpoint = projectEndpoint;
            _mapper = mapper;
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

        protected override void OnViewLoaded(object view)
        {
            // nothing local, currently.
            // children will assign themselves as the ActiveViewModel
            // and then call LoadTasks()
        }

        protected async Task LoadTasks()
        {
            Task<List<TaskModel>> loadTaskToCall = null;

            switch (ActiveViewModel)
            {
                case ViewModelChildren.InboxVM:
                    loadTaskToCall = _taskEndpoint.GetInboxTasksForUser();
                    break;
                default:
                    loadTaskToCall = _taskEndpoint.GetAllTasksForUser();
                    break;
            }

            var taskList = await loadTaskToCall;
            TasksLastFetch = taskList; // store for comparison

            var displayTaskList = _mapper.Map<List<TaskDisplayModel>>(taskList);
            Tasks = new BindingList<TaskDisplayModel>(displayTaskList);

            foreach (TaskDisplayModel displayTask in Tasks)
            {
                displayTask.PropertyChanged += onDisplayTaskPropertyChanged; // subscribe to property changed event
            }

            var projectList = await _projectEndpoint.GetAllProjectsForUser();
            Projects = new BindingList<ProjectModel>(projectList);

            // new task input placeholder
            List<TaskDisplayModel> newTaskList = new List<TaskDisplayModel>();
            TaskDisplayModel newTaskPlaceholder = new TaskDisplayModel();
            NewTask = newTaskPlaceholder;
            newTaskList.Add(newTaskPlaceholder);
            NewTaskList = new BindingList<TaskDisplayModel>(newTaskList);
        }

        public bool HasTaskDataChanged(TaskModel frontEndTask)
        {
            TaskModel taskLastFetch = TasksLastFetch.Find(x => x.Id == frontEndTask.Id);

            bool IsDataEqual(TaskModel taskA, TaskModel taskB)
            {
                return taskA.TaskName == taskB.TaskName &&
                    taskA.Completed == taskB.Completed &&
                    taskA.ProjectName == taskB.ProjectName &&
                    taskA.ContextName == taskB.ContextName &&
                    taskA.DueDate == taskB.DueDate;
            }

            return !IsDataEqual(frontEndTask, taskLastFetch);
        }

        public bool HasTaskProjectNameChanged(TaskModel frontEndTask)
        {
            TaskModel taskLastFetch = TasksLastFetch.Find(x => x.Id == frontEndTask.Id);
            return frontEndTask.ProjectName != taskLastFetch.ProjectName;
        }

        public bool HasTaskContextNameChanged(TaskModel frontEndTask)
        {
            TaskModel taskLastFetch = TasksLastFetch.Find(x => x.Id == frontEndTask.Id);
            return frontEndTask.ContextName != taskLastFetch.ContextName;
        }

        public void AssignProjectIdFromProjectName(TaskModel task)
        {
            if (task.ProjectName != null)
            {
                // lookup projectId by projectName and assign
                // TODO: need to enforce uniqueness of the projectName property - casing, etc; something. ensure these will match!
                List<ProjectModel> userProjects = Projects.ToList();

                ProjectModel assignedProject = userProjects.Find(x => x.ProjectName == task.ProjectName);
                if (assignedProject == null)
                {
                    // TODO: create new project + add to db, returning the projectId from that call, and use it here
                }
                else
                {
                    task.ProjectId = assignedProject.Id;
                }
            }
        }

        public async Task AddTask()
        {
            // map from TaskDisplayModel to TaskModel
            TaskModel newTask = _mapper.Map<TaskModel>(NewTask);

            if (newTask.ProjectName != null)
            {
                AssignProjectIdFromProjectName(newTask);
                // TODO: need to enforce uniqueness of the projectName property - casing, etc; something. ensure these will match!
            }

            if (newTask.ContextName != null)
            {
                // lookup contextId by contextName and assign
                // TODO: need to enforce uniqueness of the contextName property - casing, etc; something. ensure these will match!
            }

            // TODO: remove hard-coding of userId, obviously
            await _taskEndpoint.AddTask(newTask, "8f3b305e-ebc0-439e-a23b-6661901e4f7d");

            // refresh Tasks + clear NewTask
            await LoadTasks();
        }

        // post updated task data to API for a single displayTask
        public async Task UpdateTaskData(TaskDisplayModel displayTask)
        {
            // map from TaskDisplayModel to TaskModel
            TaskModel task = _mapper.Map<TaskModel>(displayTask);

            if (HasTaskDataChanged(task))
            {
                if (HasTaskProjectNameChanged(task))
                {
                    AssignProjectIdFromProjectName(task);
                }

                if (HasTaskContextNameChanged(task))
                {
                    // TODO: same for context
                }

                await _taskEndpoint.UpdateTask(task);

                // refresh Tasks + repopulate TasksLastFetch
                await LoadTasks();
            }
        }

        public async Task UpdateAllPendingTaskData()
        {
            // map from TaskDisplayModel to TaskModel
            List<TaskModel> allTasks = _mapper.Map<List<TaskModel>>(Tasks);

            bool somethingWasUpdated = false;

            foreach (var task in allTasks)
            {
                if (HasTaskDataChanged(task))
                {
                    if (HasTaskProjectNameChanged(task))
                    {
                        AssignProjectIdFromProjectName(task);
                    }

                    if (HasTaskContextNameChanged(task))
                    {
                        // TODO: same for context
                    }

                    await _taskEndpoint.UpdateTask(task);
                    somethingWasUpdated = true;
                }
            }

            if (somethingWasUpdated)
            {
                // refresh Tasks + repopulate TasksLastFetch
                await LoadTasks();
            }
        }

        // saves updated task data to server on property change
        private async void onDisplayTaskPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string changedProperty = e.PropertyName;
            TaskDisplayModel senderTask = (TaskDisplayModel)sender;

            await UpdateTaskData(senderTask);
        }
    }
}
