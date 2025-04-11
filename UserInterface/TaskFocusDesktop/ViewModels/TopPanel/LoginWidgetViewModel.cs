using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using System.Windows;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Models;
using Windows.Security.Credentials;

namespace TaskFocusDesktop.ViewModels.TopPanel
{
    public class LoginWidgetViewModel : ViewModelBase
    {
        private string _username = string.Empty;
        private string _password = string.Empty;
        private IAPIHelper _apiHelper;
        protected IWindowManager _window;
        protected IDataService _dataService;
        private string? _errorMessage;
        private string _resourceName = "TaskFocus";
        private bool _storedCredentialsWereFound = false;

        public LoginWidgetViewModel(IAPIHelper aPIHelper,
                            IWindowManager window,
                            IEventAggregator events,
                            IAppState appState,
                            IDataService dataService) : base(events, appState)
        {
            _apiHelper = aPIHelper;
            _window = window;
            _dataService = dataService;
        }

        private bool _enableLoginFormControls;
        public bool EnableLoginFormControls
        {
            get => _enableLoginFormControls;
            set 
            { 
                _enableLoginFormControls = value; 
                NotifyOfPropertyChange(() => EnableLoginFormControls);
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                NotifyOfPropertyChange(() => Username);
                NotifyOfPropertyChange(() => CanLogIn);
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                NotifyOfPropertyChange(() => Password);
                NotifyOfPropertyChange(() => CanLogIn);
            }
        }

        public bool IsErrorMsgVisible
        {
            get => !string.IsNullOrEmpty(ErrorMessage);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                NotifyOfPropertyChange(() => IsErrorMsgVisible);
                NotifyOfPropertyChange(() => ErrorMessage);
            }
        }

        public bool CanLogIn
        {
            get => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        private PasswordCredential? GetCredentialFromLocker()
        {
            PasswordCredential credential = null;
            var vault = new PasswordVault();
            IReadOnlyList<PasswordCredential> credentialList = null;

            try
            {
                credentialList = vault.FindAllByResource(_resourceName);
            }
            catch (Exception) { return null; }

            if (credentialList.Count > 0)
            {
                credential = credentialList[0];
            }

            return credential;
        }

        protected override async void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            try
            {
                // check for stored credentials
                var loginCredential = GetCredentialFromLocker();
                if (loginCredential != null)
                {
                    // stored credential found in the locker
                    // populate the Password property for automatic login
                    _storedCredentialsWereFound = true;
                    loginCredential.RetrievePassword();
                    Username = loginCredential.UserName;
                    Password = loginCredential.Password;
                }
                if (_appState.ShouldAutoLogin) { await LogIn(); }
                else { EnableLoginFormControls = true; }
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

        public async Task LogIn()
        {
            try
            {
                ErrorMessage = null;
                EnableLoginFormControls = false;
                // raise show login notify message event for home view to handle (auth processing, please wait...)
                await _events.PublishOnUIThreadAsync(new LoginNotifyEvent());

                // run pre-login checks
                UserModel attemptLoginUserModel = new UserModel { Email = Username };
                bool exists = await _dataService.CheckUserExists(attemptLoginUserModel);
                if (!exists)
                {
                    _appState.AlertMessage = "No matching user found.";
                    EnableLoginFormControls = true;
                    await _events.PublishOnUIThreadAsync(new AuthErrorNotifyEvent());
                }
                else
                {
                    bool confirmed = await _dataService.CheckUserEmailConfirmed(attemptLoginUserModel);
                    if (!confirmed)
                    {
                        _appState.AlertMessage = "Please confirm your email address before logging in.";
                        await _events.PublishOnUIThreadAsync(new UnconfirmedEmailNotifyEvent(attemptLoginUserModel.Email));
                    }
                    else
                    {
                        // attempt log in
                        var result = await _apiHelper.AuthenticateAsync(Username, Password);
                        if (result == null)
                        {
                            _appState.AlertMessage = "There was an error when attempting to log in. Please try again.";
                            EnableLoginFormControls = true;
                            await _events.PublishOnUIThreadAsync(new AuthErrorNotifyEvent());
                        }
                        else
                        {
                            // capture user info
                            await _apiHelper.GetLoggedInUserInfoAsync(result.AccessToken);

                            // save the credential to the credential manager
                            if (!_storedCredentialsWereFound)
                            {
                                var vault = new Windows.Security.Credentials.PasswordVault();
                                vault.Add(new Windows.Security.Credentials.PasswordCredential(
                                    _resourceName, Username, Password));
                            }                         
                            else
                            {
                                // update saved credentials if logged in with updated ones
                                var loginCredential = GetCredentialFromLocker();
                                if (Password != loginCredential!.Password)
                                {
                                    var vault = new Windows.Security.Credentials.PasswordVault();
                                    vault.Remove(loginCredential);
                                    vault.Add(new Windows.Security.Credentials.PasswordCredential(
                                    _resourceName, Username, Password));
                                }                           
                            }

                            // raise auth status log on event for shell view to handle
                            await _events.PublishOnUIThreadAsync(new AuthStatusChangedEvent(true));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                EnableLoginFormControls = true;

                Console.WriteLine(ex.Message.ToString());
                _appState.AlertMessage = ex.Message;
            }
        }
    }
}
