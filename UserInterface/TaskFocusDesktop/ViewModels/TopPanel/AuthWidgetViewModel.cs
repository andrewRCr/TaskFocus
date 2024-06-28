using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private ILoggedInUserModel _loggedInUser;
        //private IEventAggregator _events;
        private string _errorMessage;

        public AuthWidgetViewModel(IAPIHelper aPIHelper, ILoggedInUserModel loggedInUser, IEventAggregator events) : base(events)
        {
            _apiHelper = aPIHelper;
            _loggedInUser = loggedInUser;
            //_events = events;
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

        public async Task LogOut()
        {
            try
            {
                ErrorMessage = null;

                _apiHelper.LogOutUser();
                _loggedInUser.ResetUserModel();

                // raise log off event for shell view to handle
                await _events.PublishOnUIThreadAsync(new AuthStatusChangedEvent(false));
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
    }
}
