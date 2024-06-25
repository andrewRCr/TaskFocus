using AutoMapper;
using Caliburn.Micro;
using System;
using System.ComponentModel;
using System.Dynamic;
using System.Windows;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.MainContent
{
    public class ProjectsViewModel : TaskViewModelBase, INotifyPropertyChanged
    {
        public ProjectsViewModel(IAPIHelper apiHelper,
                      IUserEndpoint userEndpoint,
                      ITaskEndpoint taskEndpoint,
                      IProjectEndpoint projectEndpoint,
                      IContextEndpoint contextEndpoint,
                      IMapper mapper,
                      IDataHelper dataHelper,
                      IWindowManager window) : base(apiHelper, userEndpoint, taskEndpoint, projectEndpoint, contextEndpoint, mapper, dataHelper, window)
        {
        }

        protected override async void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            try
            {
                ActiveViewModel = ViewModelChildren.ProjectsVM;
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
