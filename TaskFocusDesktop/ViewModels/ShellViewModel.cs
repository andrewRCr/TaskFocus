using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using TaskFocusDesktop.EventModels;

namespace TaskFocusDesktop.ViewModels
{
    public class ShellViewModel : Conductor<object>, IHandle<LogOnEvent>
    {
        private IEventAggregator _events;
        private SimpleContainer _container;
        private InboxViewModel _inboxVM;

        public ShellViewModel(IEventAggregator events, SimpleContainer container, InboxViewModel inboxVM)
        {
            _events = events;
            _container = container;
            _inboxVM = inboxVM;

            _events.SubscribeOnUIThread(this);

            ActivateItemAsync(_container.GetInstance<LoginViewModel>());
        }

        public Task HandleAsync(LogOnEvent message, CancellationToken cancellationToken)
        {
            ActivateItemAsync(_inboxVM);

            return Task.CompletedTask;
        }
    }
}
