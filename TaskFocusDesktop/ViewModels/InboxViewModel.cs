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

        public InboxViewModel(ITaskEndpoint taskEndpoint)
        {
			_taskEndpoint = taskEndpoint;
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
	}
}
