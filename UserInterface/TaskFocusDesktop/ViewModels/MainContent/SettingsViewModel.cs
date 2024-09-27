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
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Utilities;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.MainContent
{
    public class SettingsViewModel : TaskViewModelBase, INotifyPropertyChanged
    {
        private UserSettingsDisplayModel _localSettings = default!;
        public UserSettingsDisplayModel LocalSettings
        {
            get { return _localSettings; }
            set 
            { 
                _localSettings = value; 
                NotifyOfPropertyChange(() => LocalSettings);
            }
        }

        private UserModel _localCurrentUser = default!;
        public UserModel LocalCurrentUser
        {
            get { return _localCurrentUser; }
            set 
            { 
                _localCurrentUser = value; 
                NotifyOfPropertyChange(() => LocalCurrentUser);
            }
        }

        private string _cleanDaysTextStr;
        public string CleanDaysTextStr
        {
            get { return _cleanDaysTextStr; }
            set 
            {
                _cleanDaysTextStr = value;
                NotifyOfPropertyChange(() => CleanDaysTextStr);
            }
        }

        private string _deleteDaysTextStr;
        public string DeleteDaysTextStr
        {
            get { return _deleteDaysTextStr; }
            set 
            {
                _deleteDaysTextStr = value; 
                NotifyOfPropertyChange(() => DeleteDaysTextStr);
            }
        }

        public SettingsViewModel(IEventAggregator events, IAppState appState, IWindowManager window,
                                 IDataState dataState, IDataService dataService, IDataHelper dataHelper) : base(events, appState, window, dataState, dataService, dataHelper)
        {
        }

        public RelayCommand RequestUpdateEmailDialogCommand => new RelayCommand(async execute => await RequestUpdateEmailDialog());
        public RelayCommand RequestChangePasswordDialogCommand => new RelayCommand(async execute => await RequestChangePasswordDialog());

        private async Task RequestUpdateEmailDialog()
        {
            var requestShowDialogEvent = new RequestShowDialogEvent(ViewCatalog.DialogView.UpdateEmailDialog);
            await _events.PublishOnUIThreadAsync(requestShowDialogEvent);
        }

        private async Task RequestChangePasswordDialog()
        {
            var requestShowDialogEvent = new RequestShowDialogEvent(ViewCatalog.DialogView.ChangePasswordDialog);
            await _events.PublishOnUIThreadAsync(requestShowDialogEvent);
        }

        protected override async void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            try
            {
                LoadLocalSettingsData();
                LoadLocalUserData();
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

        protected void LoadLocalSettingsData()
        {
            if (_dataState.IsDataLoaded())
            {
                LocalSettings = _dataState.UserSettings;
                LocalSettings.PropertyChanged += OnExistingSettingsPropertyChanged!; // subscribe to property changed event
                //SetRadioTextColor();
                CleanDaysTextStr = LocalSettings.CleanUpDelayDays > 1 ? "days after completion" : "day after completion";
                DeleteDaysTextStr = LocalSettings.DeleteDelayDays > 1 ? "days after completion" : "day after completion";
            }
        }

        protected void LoadLocalUserData()
        {
            if (_dataState.IsDataLoaded() && _dataState.CurrentUser != null)
            {
                LocalCurrentUser = _dataState.CurrentUser;

                //_userModel.Id = LocalCurrentUser.Id;
                //_userModel.FirstName = LocalCurrentUser.FirstName;
                //_userModel.LastName = LocalCurrentUser.LastName;
                //_userModel.Email = LocalCurrentUser.Email;
                //_authUserModel.FirstName = LocalCurrentUser.FirstName;
                //_authUserModel.LastName = LocalCurrentUser.LastName;
                //_authUserModel.Email = LocalCurrentUser.Email;
            }
        }

        // saves updated user settings data to server on property change
        protected async void OnExistingSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string? changedProperty = e.PropertyName;
            UserSettingsDisplayModel senderSettings = (UserSettingsDisplayModel)sender;
            Console.WriteLine($"LocalSettings's property {changedProperty} was changed.");

            await _dataService.UpdateSettingsData(senderSettings);
        }

        protected override bool HandleDataStateChanged(string propertyName, IDataState dataState)
        {
            if (!dataRefreshTriggers.Contains(propertyName))
            {
                return false;
            }

            //Console.WriteLine("SettingsViewPage: returned true on HandleDataStateChanged!");
            LoadLocalSettingsData();

            return true;
        }
    }
}
