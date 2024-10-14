using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.ViewModels.Base;

namespace TaskFocusDesktop.ViewModels.MainContent
{
    public class HomeViewModel : ViewModelBase, IHandle<LoginNotifyEvent>
    {
        private bool _showPleaseWaitLoginMessage = false;
        public bool ShowPleaseWaitLoginMessage
        {
            get { return _showPleaseWaitLoginMessage; }
            set
            {
                _showPleaseWaitLoginMessage = value;
                NotifyOfPropertyChange(() => ShowPleaseWaitLoginMessage);
            }
        }

        private bool _showNotAuthenticatedMessage;
        public bool ShowNotAuthenticatedMessage
        {
            get { return _showNotAuthenticatedMessage; }
            set 
            { 
                _showNotAuthenticatedMessage = value; 
                NotifyOfPropertyChange(() => ShowNotAuthenticatedMessage);
            }
        }

        public HomeViewModel(IEventAggregator events, IAppState appState) : base(events, appState)
        {
            _events = events;
            _events.SubscribeOnPublishedThread(this);

            ShowNotAuthenticatedMessage = !_appState.IsAuthenticated;
        }

        Task IHandle<LoginNotifyEvent>.HandleAsync(LoginNotifyEvent message, CancellationToken cancellationToken)
        {
            ShowPleaseWaitLoginMessage = true;
            return Task.CompletedTask;
        }
    }
}
