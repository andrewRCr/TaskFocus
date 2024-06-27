using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.ViewModels.Base;

namespace TaskFocusDesktop.ViewModels.SidePanel
{
    public class NavMenuViewModel : ViewModelBase
    {
        public NavMenuViewModel(IEventAggregator events, AppState appState) : base(events, appState)
        {
        }

        public ICommand SwitchToInboxViewCommand => new RelayCommand(async execute => await RequestSwitchToInboxView());

        public ICommand SwitchToTodayViewCommand => new RelayCommand(async execute => await RequestSwitchToTodayView());

        public ICommand SwitchToProjectsViewCommand => new RelayCommand(async execute => await RequestSwitchToProjectsView());

        public ICommand SwitchToContextsViewCommand => new RelayCommand(async execute => await RequestSwitchToContextsView());
            
        public ICommand SwitchToCompletedViewCommand => new RelayCommand(async execute => await RequestSwitchToCompletedView());


        private async Task RequestSwitchToInboxView()
        {
            _appState.ActiveMainContentView = AppState.MainContentView.Inbox;
            NotifyOfPropertyChange(() => ActiveAppStateMainContentView);
            await _events.PublishOnUIThreadAsync(new MainContentViewSwitchEvent());
        }

        private async Task RequestSwitchToTodayView()
        {
            _appState.ActiveMainContentView = AppState.MainContentView.Today;
            NotifyOfPropertyChange(() => ActiveAppStateMainContentView);
            await _events.PublishOnUIThreadAsync(new MainContentViewSwitchEvent());
        }

        private async Task RequestSwitchToProjectsView()
        {
            _appState.ActiveMainContentView = AppState.MainContentView.Projects;
            NotifyOfPropertyChange(() => ActiveAppStateMainContentView);
            await _events.PublishOnUIThreadAsync(new MainContentViewSwitchEvent());
        }

        private async Task RequestSwitchToContextsView()
        {
            _appState.ActiveMainContentView = AppState.MainContentView.Contexts;
            NotifyOfPropertyChange(() => ActiveAppStateMainContentView);
            await _events.PublishOnUIThreadAsync(new MainContentViewSwitchEvent());
        }

        private async Task RequestSwitchToCompletedView()
        {
            _appState.ActiveMainContentView = AppState.MainContentView.Completed;
            NotifyOfPropertyChange(() => ActiveAppStateMainContentView);
            await _events.PublishOnUIThreadAsync(new MainContentViewSwitchEvent());
        }
    }
}
