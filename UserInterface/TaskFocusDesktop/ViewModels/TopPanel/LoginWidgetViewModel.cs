using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library.API;
using Windows.Security.Credentials;

namespace TaskFocusDesktop.ViewModels.TopPanel
{
    public class LoginWidgetViewModel : ViewModelBase
    {
        private string _username = string.Empty;
        private string _password = string.Empty;
        private IAPIHelper _apiHelper;
        protected IWindowManager _window;
        private string? _errorMessage;
        private string _resourceName = "TaskFocus";
        private string? _defaultUserName;
        private bool _storedCredentialsWereFound = false;

        public LoginWidgetViewModel(IAPIHelper aPIHelper, IWindowManager window, IEventAggregator events) : base(events)
        {
            _apiHelper = aPIHelper;
            _window = window;
        }

        private bool _enableLoginFormControls;
        public bool EnableLoginFormControls
        {
            get { return _enableLoginFormControls; }
            set 
            { 
                _enableLoginFormControls = value; 
                NotifyOfPropertyChange(() => EnableLoginFormControls);
            }
        }

        public string Username
        {
            get { return _username; }
            set
            {
                _username = value;
                NotifyOfPropertyChange(() => Username);
                NotifyOfPropertyChange(() => CanLogIn);
            }
        }

        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                NotifyOfPropertyChange(() => Password);
                NotifyOfPropertyChange(() => CanLogIn);
            }
        }

        public bool IsErrorMsgVisible
        {
            get
            {
                return !string.IsNullOrEmpty(ErrorMessage);
            }
        }

        public string? ErrorMessage
        {
            get { return _errorMessage; }
            set
            {
                _errorMessage = value;
                NotifyOfPropertyChange(() => IsErrorMsgVisible);
                NotifyOfPropertyChange(() => ErrorMessage);
            }
        }

        public bool CanLogIn
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
            }
        }

        public async Task LogIn()
        {
            try
            {
                ErrorMessage = null;
                EnableLoginFormControls = false;
                // raise show login notify message event for home view to handle
                await _events.PublishOnUIThreadAsync(new LoginNotifyEvent());
                // log in
                var result = await _apiHelper.AuthenticateAsync(Username, Password);
                // capture user info
                await _apiHelper.GetLoggedInUserInfoAsync(result.AccessToken);

                // save the credential to the credential manager
                if (!_storedCredentialsWereFound)
                {
                    var vault = new Windows.Security.Credentials.PasswordVault();
                    vault.Add(new Windows.Security.Credentials.PasswordCredential(
                        _resourceName, Username, Password));
                }

                // raise auth status log on event for shell view to handle
                await _events.PublishOnUIThreadAsync(new AuthStatusChangedEvent(true));
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                EnableLoginFormControls = true;
            }
        }

        protected override async void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            EnableLoginFormControls = false;

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
                    await LogIn();
                }
                else // allow manual UI login
                {
                    EnableLoginFormControls = true;
                }
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

        private Windows.Security.Credentials.PasswordCredential? GetCredentialFromLocker()
        {
            Windows.Security.Credentials.PasswordCredential credential = null;
            var vault = new Windows.Security.Credentials.PasswordVault();
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
    }
}
