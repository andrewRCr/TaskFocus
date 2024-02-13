using AutoMapper;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
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
        public InboxViewModel(IUserEndpoint userEndpoint, ITaskEndpoint taskEndpoint, IProjectEndpoint projectEndpoint,
            IMapper mapper, IWindowManager window) : base(userEndpoint, taskEndpoint, projectEndpoint, mapper, window)
        {
        }

        protected override async void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            try
            {
	            ActiveViewModel = ViewModelChildren.InboxVM;
	            await LoadTasks();
            }
            catch (Exception ex)
            {
                dynamic settings = new ExpandoObject();
                settings.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                settings.ResizeMode = ResizeMode.NoResize;
                settings.Title = "Exception!";

                var status = IoC.Get<StatusInfoViewModel>();
                status.UpdateMessage($"{ex.Source} threw an exception:", ex.Message);
                await _window.ShowDialogAsync(status, null, settings);
                await TryCloseAsync();
            }
        }
    }
}
