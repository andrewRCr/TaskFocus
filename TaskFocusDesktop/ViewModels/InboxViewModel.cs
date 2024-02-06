using AutoMapper;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;
using System.Windows;
using System.Windows.Controls;
using TaskFocusDesktop.Library.API;
using TaskFocusDesktop.Library.Models;
using TaskFocusDesktop.Models;
using static System.Net.Mime.MediaTypeNames;

namespace TaskFocusDesktop.ViewModels
{
    public class InboxViewModel : Screen
    {
		ITaskEndpoint _taskEndpoint;
		IProjectEndpoint _projectEndpoint;
        IMapper _mapper;

        public InboxViewModel(ITaskEndpoint taskEndpoint, IProjectEndpoint projectEndpoint,
            IMapper mapper)
        {
			_taskEndpoint = taskEndpoint;
			_projectEndpoint = projectEndpoint;
            _mapper = mapper;
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

		private string _selectedProjectNameForTask;
		public string SelectedProjectNameForTask
        {
            get { return _selectedProjectNameForTask; }
            set
            {
                _selectedProjectNameForTask = value;
                NotifyOfPropertyChange(() => SelectedProjectNameForTask);
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

        protected override async void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            await LoadTasks();
        }

        private async Task LoadTasks()
        {
            var taskList = await _taskEndpoint.GetAllForUser();
            TasksLastFetch = taskList; // store for comparison

            var displayTaskList = _mapper.Map<List<TaskDisplayModel>>(taskList);
            Tasks = new BindingList<TaskDisplayModel>(displayTaskList);

            var projectList = await _projectEndpoint.GetAllForUser();
            Projects = new BindingList<ProjectModel>(projectList);

            // new task input placeholder
            List<TaskDisplayModel> newTaskList = new List<TaskDisplayModel>();
            TaskDisplayModel newTaskPlaceholder = new TaskDisplayModel();
            newTaskList.Add(newTaskPlaceholder);
            NewTaskList = new BindingList<TaskDisplayModel>(newTaskList);
        }

        public async Task AddTask()
		{
            if (NewTask.ProjectName != null)
            {
                // lookup projectId by projectName and assign
                // TODO: need to enforce uniqueness of the projectName property - casing, etc; something. ensure these will match!
                List<ProjectModel> userProjects = Projects.ToList();

                ProjectModel assignedProject = userProjects.Find(x => x.ProjectName == NewTask.ProjectName);
                NewTask.ProjectId = assignedProject.Id;
            }

            if (NewTask.ContextName != null)
            {
                // lookup contextId by contextName and assign
                // TODO: need to enforce uniqueness of the contextName property - casing, etc; something. ensure these will match!
                //List<ContextModel> userContexts = Contexts.ToList();

                //ContextModel assignedContext = userContexts.Find(x => x.ContextName == NewTask.ContextName);
                //NewTask.ContextId = assignedContext.Id;
            }

            // map from TaskDisplayModel to TaskModel and add
            TaskModel newTask = _mapper.Map<TaskModel>(NewTask);
            await _taskEndpoint.AddTask(newTask, "8f3b305e-ebc0-439e-a23b-6661901e4f7d");

            // refresh Tasks + clear NewTask
            await LoadTasks();
        }

        // post updated task data to API
        public async Task UpdateTaskData()
		{
            // map from TaskDisplayModel to TaskModel
            TaskModel selectedTask = _mapper.Map<TaskModel>(SelectedTask);

            await _taskEndpoint.UpdateTask(selectedTask);
        }

        public async Task UpdateAllPendingTaskData()
        {
            // map from TaskDisplayModel to TaskModel
            List<TaskModel> allTasks =_mapper.Map<List<TaskModel>>(Tasks);

            bool somethingWasUpdated = false;

            foreach (var task in allTasks)
            {
                if (HasTaskDataChanged(task))
                {
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
    }
}
