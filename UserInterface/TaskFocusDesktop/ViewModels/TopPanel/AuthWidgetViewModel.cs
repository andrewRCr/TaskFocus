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
    public class AuthWidgetViewModel : ViewModelBase
    {
        private IEventAggregator _events;
        private string _errorMessage;

        public AuthWidgetViewModel(IEventAggregator events)
        {
            _events = events;
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
                await _events.PublishOnUIThreadAsync(new LogOffEvent());
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
    }
}
