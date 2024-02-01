using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFocusDesktop.ViewModels
{
    public class InboxViewModel : Screen
    {
		private BindingList<string> _tasks;

		public BindingList<string> Tasks
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
