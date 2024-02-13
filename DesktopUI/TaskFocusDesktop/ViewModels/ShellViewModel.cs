using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Library.API;
using TaskFocusDesktop.Library.Models;

namespace TaskFocusDesktop.ViewModels
{
    public class ShellViewModel : Conductor<object>, IHandle<LogOnEvent>
    {
        private IAPIHelper _apiHelper;
        private ILoggedInUserModel _loggedInUser;
        private IEventAggregator _events;
        private InboxViewModel _inboxVM;

        public ShellViewModel(IAPIHelper apiHelper, ILoggedInUserModel loggedInUser, IEventAggregator events, InboxViewModel inboxVM)
        {
            _apiHelper = apiHelper;
            _loggedInUser = loggedInUser;
            _events = events;
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
            _apiHelper.LogOutUser();
            _loggedInUser.ResetUserModel();
            NotifyOfPropertyChange(() => IsUserLoggedIn);
            ActivateItemAsync(IoC.Get<LoginViewModel>());
        }

        public Task HandleAsync(LogOnEvent message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(() => IsUserLoggedIn);
            ActivateItemAsync(_inboxVM);

            return Task.CompletedTask;
        }

        public void SwitchToInboxView()
        {
            ActivateItemAsync(IoC.Get<InboxViewModel>());
        }

        public void SwitchToTodayView() 
        {
            ActivateItemAsync(IoC.Get<TodayViewModel>());
        }
    }
}
