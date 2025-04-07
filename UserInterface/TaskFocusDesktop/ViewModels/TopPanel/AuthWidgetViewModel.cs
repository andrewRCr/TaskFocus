using Caliburn.Micro;
using System;
using System.Threading.Tasks;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.ViewModels.TopPanel
{
    public class AuthWidgetViewModel : ViewModelBase
    {
        private IAPIHelper _apiHelper;
        private ILoggedInUserModel? _loggedInUser;

        private string? _userEmailAddressStr;
        public string? UserEmailAddressStr
        {
            get { return _userEmailAddressStr; }
            set 
            { 
                _userEmailAddressStr = value;
                NotifyOfPropertyChange(() => UserEmailAddressStr);
            }
        }

        private string? _errorMessage;
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

        public bool IsErrorMsgVisible
        {
            get
            {
                return !string.IsNullOrEmpty(ErrorMessage);
            }
        }

        public AuthWidgetViewModel(IAPIHelper aPIHelper, ILoggedInUserModel loggedInUser, IEventAggregator events, IAppState appState) : base(events, appState)
        {
            _apiHelper = aPIHelper;
            _loggedInUser = loggedInUser;

            if (_loggedInUser != null)
            {
                UserEmailAddressStr = _loggedInUser.Email;
            }
        }

        public async Task LogOut()
        {
            try
            {
                ErrorMessage = null;

                //_apiHelper.LogOutUser();
                _loggedInUser!.ResetUserModel();
                UserEmailAddressStr = null;

                // raise logout event for LoginWidget to handle 
                await _events.PublishOnUIThreadAsync(new LogoutNotifyEvent());
                // raise auth status changed event for ShellView to handle
                await _events.PublishOnUIThreadAsync(new AuthStatusChangedEvent(false));
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
    }
}
