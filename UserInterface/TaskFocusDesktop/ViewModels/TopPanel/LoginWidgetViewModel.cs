using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private IEventAggregator _events;
        private string _errorMessage;

        public LoginWidgetViewModel(IAPIHelper aPIHelper, IEventAggregator events)
        {
            _apiHelper = aPIHelper;
            _events = events;
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

                await _events.PublishOnUIThreadAsync(new LogOnEvent());
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
    }
}
