using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskFocusDesktop.Library.API;
using TaskFocusDesktop.Library.Models;

namespace TaskFocusDesktop.ViewModels
{
    public class InboxViewModel : Screen
    {
		ITaskEndpoint _taskEndpoint;
		IProjectEndpoint _projectEndpoint;

        public InboxViewModel(ITaskEndpoint taskEndpoint, IProjectEndpoint projectEndpoint)
        {
			_taskEndpoint = taskEndpoint;
			_projectEndpoint = projectEndpoint;
        }

        protected override async void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            await LoadTasks();
        }

        private async Task LoadTasks()
		{
            var taskList = await _taskEndpoint.GetAllForUser();
            Tasks = new BindingList<TaskModel>(taskList);

			var projectList = await _projectEndpoint.GetAllForUser();
			Projects = new BindingList<ProjectModel>(projectList);
        }

        private BindingList<TaskModel> _tasks;

		public BindingList<TaskModel> Tasks
		{
			get { return _tasks; }
			set 
			{ 
				_tasks = value;
				NotifyOfPropertyChange(() => Tasks);
			}
		}

		private TaskModel _selectedTask;

		public TaskModel SelectedTask
		{
			get { return _selectedTask; }
			set 
			{ 
				_selectedTask = value; 
				NotifyOfPropertyChange(() => SelectedTask);
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
	}
}
