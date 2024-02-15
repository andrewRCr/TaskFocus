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

            _events.SubscribeOnPublishedThread(this);

            ActivateItemAsync(IoC.Get<LoginViewModel>(), new CancellationToken());
        }

        public bool IsUserLoggedIn
        {
            get
            {
                return !string.IsNullOrWhiteSpace(_loggedInUser.Token);
            }
        }

        public async Task ExitApplication()
        {
            await TryCloseAsync();
        }

        public async Task LogOut()
        {
            _apiHelper.LogOutUser();
            _loggedInUser.ResetUserModel();
            await ActivateItemAsync(IoC.Get<LoginViewModel>(), new CancellationToken());
            NotifyOfPropertyChange(() => IsUserLoggedIn);
        }

        public async Task HandleAsync(LogOnEvent message, CancellationToken cancellationToken)
        {
            await ActivateItemAsync(_inboxVM, cancellationToken);
            NotifyOfPropertyChange(() => IsUserLoggedIn);
        }

        public async Task SwitchToInboxView()
        {
            await ActivateItemAsync(IoC.Get<InboxViewModel>(), new CancellationToken());
        }

        public async Task SwitchToTodayView() 
        {
            await ActivateItemAsync(IoC.Get<TodayViewModel>(), new CancellationToken());
        }
    }
}
