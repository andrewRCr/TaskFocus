using AutoMapper;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using TaskFocusDesktop.Library.API;
using TaskFocusDesktop.Library.Models;
using TaskFocusDesktop.Models;
using static System.Net.Mime.MediaTypeNames;

namespace TaskFocusDesktop.ViewModels
{
    public class InboxViewModel : TaskViewModelBase, INotifyPropertyChanged
    {
        public InboxViewModel(ITaskEndpoint taskEndpoint, IProjectEndpoint projectEndpoint,
            IMapper mapper) : base(taskEndpoint, projectEndpoint, mapper)
        {
        }

        protected override async void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            ActiveViewModel = ViewModelChildren.InboxVM;
            await LoadTasks();
        }
    }
}
