using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Library.Models;

namespace TaskFocusDesktop.ViewModels
{
    public class ShellViewModel : Conductor<object>, IHandle<LogOnEvent>
    {
        private IEventAggregator _events;
        private ILoggedInUserModel _loggedInUser;
        private InboxViewModel _inboxVM;

        public ShellViewModel(IEventAggregator events, ILoggedInUserModel loggedInUser, InboxViewModel inboxVM)
        {
            _events = events;
            _loggedInUser = loggedInUser;
            _inboxVM = inboxVM;

            _events.SubscribeOnUIThread(this);

            ActivateItemAsync(IoC.Get<LoginViewModel>());
        }

        public bool IsUserLoggedIn
        {
            get
            {
                return !string.IsNullOrWhiteSpace(_loggedInUser.Token);
            }
        }

        public void ExitApplication()
        {
            TryCloseAsync();
        }

        public void LogOut()
        {
            _loggedInUser.LogOutUser();
            NotifyOfPropertyChange(() => IsUserLoggedIn);
            ActivateItemAsync(IoC.Get<LoginViewModel>());

        }

        public Task HandleAsync(LogOnEvent message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(() => IsUserLoggedIn);
            ActivateItemAsync(_inboxVM);

            return Task.CompletedTask;
        }
    }
}
