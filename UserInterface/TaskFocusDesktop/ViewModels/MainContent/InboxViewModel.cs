using AutoMapper;
using Caliburn.Micro;
using System;
using System.ComponentModel;
using System.Dynamic;
using System.Windows;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.MainContent
{
    public class InboxViewModel : TaskViewModelBase, INotifyPropertyChanged
    {
        public InboxViewModel(IDataState dataState,
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
                if (!_dataState.IsDataLoaded())
                {
                    await _dataService.FetchRemoteTaskData();
                }

                LoadLocalTaskData();

                //ActiveViewModel = ViewModelChildren.InboxVM;
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
