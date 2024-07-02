using Caliburn.Micro;
using System;
using System.ComponentModel;
using System.Dynamic;
using System.Windows;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.MainContent
{
    public class CompletedViewModel : TaskViewModelBase, INotifyPropertyChanged
    {
        public CompletedViewModel(IDataState dataState, 
                                  IDataService dataService,
                                  IDataHelper dataHelper,
                                  IEventAggregator events,
                                  IWindowManager window) : base(dataState, dataService, dataHelper, events, window)
        {
        }

        protected override async void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            try
            {
                //ActiveViewModel = ViewModelChildren.CompletedVM;
                //await LoadTasks();
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
