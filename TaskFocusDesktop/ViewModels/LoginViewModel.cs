using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Library.API;

namespace TaskFocusDesktop.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
		private string _username = "andrew.creekmore@me.com";
		private string _password = "Pwd12345.";
		private IAPIHelper _apiHelper;
		private IEventAggregator _events;
        private string _errorMessage;

        public LoginViewModel(IAPIHelper aPIHelper, IEventAggregator events)
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
				return !String.IsNullOrEmpty(ErrorMessage);
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
				return !String.IsNullOrWhiteSpace(Username) && !String.IsNullOrWhiteSpace(Password);
            }
		}

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            ActiveViewModel = ViewModelChildren.LoginVM;
        }

        public async Task LogIn()
		{
            try
            {
				ErrorMessage = null;
                var result = await _apiHelper.Authenticate(Username, Password);

                // capture user info
                await _apiHelper.GetLoggedInUserInfo(result.Access_Token);

				await _events.PublishOnUIThreadAsync(new LogOnEvent());
			}
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
	}
}
