using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskFocusDesktop.Library.API;

namespace TaskFocusDesktop.ViewModels
{
    public class LoginViewModel : Screen
    {
		private string _username;
		private string _password;
		private IAPIHelper _apiHelper;
        private string _errorMessage;

        public LoginViewModel(IAPIHelper aPIHelper)
        {
            _apiHelper = aPIHelper;
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

		public async Task LogIn()
		{
            try
            {
				ErrorMessage = null;
                var result = await _apiHelper.Authenticate(Username, Password);

                // capture user info
                await _apiHelper.GetLoggedInUserInfo(result.Access_Token);

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
	}
}
