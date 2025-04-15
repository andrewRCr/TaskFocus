using Caliburn.Micro;
using System;
using System.ComponentModel;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Utilities;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Data.Utilities;
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.ViewModels.MainContent
{
    public class SettingsViewModel : TaskViewModelBase, INotifyPropertyChanged
    {
        public SettingsViewModel(IEventAggregator events,
                         IAppState appState,
                         IWindowManager window,
                         IDataState dataState,
                         IDataService dataService,
                         IDataHelper dataHelper) : base(events, appState, window, dataState, dataService, dataHelper)
        {
            dataRefreshTriggers = [nameof(EDataRefreshType.User), nameof(EDataRefreshType.Settings)];
        }

        public RelayCommand RequestUpdateEmailDialogCommand => new RelayCommand(async execute => await RequestUpdateEmailDialog());
        public RelayCommand RequestChangePasswordDialogCommand => new RelayCommand(async execute => await RequestChangePasswordDialog());

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

        private UserDisplayModel _localCurrentUser = default!;
        public UserDisplayModel LocalCurrentUser
        {
            get { return _localCurrentUser; }
            set
            {
                _localCurrentUser = value;
                NotifyOfPropertyChange(() => LocalCurrentUser);
            }
        }

        private string? _cleanDaysTextStr;
        public string? CleanDaysTextStr
        {
            get { return _cleanDaysTextStr; }
            set 
            {
                _cleanDaysTextStr = value;
                NotifyOfPropertyChange(() => CleanDaysTextStr);
            }
        }

        private string? _deleteDaysTextStr;
        public string? DeleteDaysTextStr
        {
            get { return _deleteDaysTextStr; }
            set 
            {
                _deleteDaysTextStr = value; 
                NotifyOfPropertyChange(() => DeleteDaysTextStr);
            }
        }

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
            if (_dataService.IsDataStateLoaded())
            {
                LocalSettings = _dataService.GetDataStateUserSettings()!;
                LocalSettings.PropertyChanged += OnExistingSettingsPropertyChanged!; // subscribe to property changed event

                CleanDaysTextStr = LocalSettings.CleanUpDelayDays > 1 ? "days after completion" : "day after completion";
                DeleteDaysTextStr = LocalSettings.DeleteDelayDays > 1 ? "days after completion" : "day after completion";
            }
        }

        protected void LoadLocalUserData()
        {
            if (_dataService.IsDataStateLoaded())
            {
                LocalCurrentUser = _dataService.GetDataStateCurrentUser()!;
                LocalCurrentUser.PropertyChanged += OnExistingUserPropertyChanged!; // subscribe to property changed event
            }
        }

        // saves updated user settings data to local data state on property change
        protected async void OnExistingSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string? changedProperty = e.PropertyName;
            UserSettingsDisplayModel senderSettings = (UserSettingsDisplayModel)sender;

            await VerifyAuthAndRedirectIfExpired();
            _dataService.UpdateSettingsData(senderSettings);
        }

        // saves updated user data to local data state on property change
        protected async void OnExistingUserPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string? changedProperty = e.PropertyName;
            UserDisplayModel senderUser = (UserDisplayModel)sender;

            // will only be triggered by name changes; other properties handled via dialogs
            await VerifyAuthAndRedirectIfExpired();
            _dataService.UpdateUserNameData(senderUser);         
        }

        protected override bool HandleDataStateChanged(string propertyName, IDataState dataState)
        {
            if (!dataRefreshTriggers.Contains(propertyName) || ActiveMainContentView != ViewCatalog.MainContentView.Settings)
            {
                return false;
            }

            LoadLocalSettingsData();
            LoadLocalUserData();

            return true;
        }

        // ViewSwitchedEvent handler
        public override async Task HandleAsync(ViewSwitchedEvent message, CancellationToken cancellationToken)
        {
            if (message.SwitchedContentPanel == ViewCatalog.ContentPanel.MainContent)
            {
                LocalCurrentUser.PropertyChanged -= OnExistingUserPropertyChanged!; // unsubscribe from property changed events
                LocalSettings.PropertyChanged -= OnExistingSettingsPropertyChanged!;
            }

            await base.HandleAsync(message, cancellationToken);
        }
    }
}
