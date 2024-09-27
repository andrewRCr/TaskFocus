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

        public HomeViewModel(IEventAggregator events, IAppState appState) : base(events, appState)
        {
            _events = events;
            _events.SubscribeOnPublishedThread(this);
        }

        Task IHandle<LoginNotifyEvent>.HandleAsync(LoginNotifyEvent message, CancellationToken cancellationToken)
        {
            ShowPleaseWaitLoginMessage = true;
            return Task.CompletedTask;
        }
    }
}
