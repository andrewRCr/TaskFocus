using Caliburn.Micro;
using System;
using System.Threading.Tasks;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.ViewModels.TopPanel
{
    public class AuthWidgetViewModel : ViewModelBase
    {
        private ILoggedInUserModel? _loggedInUser;

        public AuthWidgetViewModel(ILoggedInUserModel loggedInUser,
                                   IEventAggregator events,
                                   IAppState appState) : base(events, appState)
        {
            _loggedInUser = loggedInUser;
            if (_loggedInUser != null) UserFirstNameStr = _loggedInUser.FirstName;      
        }

        private string? _userFirstNameStr;
        public string? UserFirstNameStr
        {
            get { return _userFirstNameStr; }
            set 
            {
                _userFirstNameStr = value;
                NotifyOfPropertyChange(() => UserFirstNameStr);
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
            get =>!string.IsNullOrEmpty(ErrorMessage);         
        }

        public async Task LogOut()
        {
            try
            {
                ErrorMessage = null;
                _loggedInUser!.ResetUserModel();
                UserFirstNameStr = null;

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
