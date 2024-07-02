using AutoMapper;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.MainContent
{
    public class SettingsViewModel : TaskViewModelBase, INotifyPropertyChanged
    {

        public SettingsViewModel(IDataState dataState,
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
                //ActiveViewModel = ViewModelChildren.SettingsVM;
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
