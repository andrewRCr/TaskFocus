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

namespace TaskFocusDesktop.ViewModels.TopPanel
{
    public class LoginWidgetViewModel : ViewModelBase
    {
        private string _username = "andrew.creekmore@me.com";
        private string _password = "pWd123.";
        private IAPIHelper _apiHelper;
        protected IWindowManager _window;
        private string _errorMessage;

        public LoginWidgetViewModel(IAPIHelper aPIHelper, IWindowManager window, IEventAggregator events) : base(events)
        {
            _apiHelper = aPIHelper;
            _window = window;
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

        public string ErrorMessage
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
                var result = await _apiHelper.AuthenticateAsync(Username, Password);

                // capture user info
                await _apiHelper.GetLoggedInUserInfoAsync(result.AccessToken);
                // raise log on event for shell view to handle
                await _events.PublishOnUIThreadAsync(new AuthStatusChangedEvent(true));
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        // TEMP / DEV ONLY
        protected override async void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            try
            {
                await LogIn();
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
