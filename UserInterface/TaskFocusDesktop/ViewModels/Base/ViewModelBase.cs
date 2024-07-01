using Caliburn.Micro;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Utilities;

namespace TaskFocusDesktop.ViewModels.Base
{
    public abstract class ViewModelBase : Screen, IHandle<ViewSwitchedEvent>
    {
        protected IEventAggregator _events;

        protected ViewModelBase(IEventAggregator events)
        {
            _events = events;
            _events.SubscribeOnPublishedThread(this);
        }

        protected ViewCatalog.MainContentView ActiveMainContentView { get; set; }

        protected ViewCatalog.SidePanelView ActiveSidePanelView { get; set; }

        public ICommand SwitchToInboxViewCommand => new RelayCommand(
            async execute => await RequestMainContentViewSwitch(ViewCatalog.MainContentView.Inbox));

        public ICommand SwitchToTodayViewCommand => new RelayCommand(
            async execute => await RequestMainContentViewSwitch(ViewCatalog.MainContentView.Today));

        public ICommand SwitchToProjectsViewCommand => new RelayCommand(
            async execute => await RequestMainContentViewSwitch(ViewCatalog.MainContentView.Projects));

        public ICommand SwitchToContextsViewCommand => new RelayCommand(
            async execute => await RequestMainContentViewSwitch(ViewCatalog.MainContentView.Contexts));

        public ICommand SwitchToCompletedViewCommand => new RelayCommand(
            async execute => await RequestMainContentViewSwitch(ViewCatalog.MainContentView.Completed));

        public ICommand SwitchToSettingsViewCommand => new RelayCommand(
                async execute => await RequestMainContentViewSwitch(ViewCatalog.MainContentView.Settings));

        public ICommand SwitchToMainNavMenuCommand => new RelayCommand(
                async execute => await RequestSidePanelViewSwitch(ViewCatalog.SidePanelView.NavMenu));

        public ICommand SwitchToSubNavMenuCommand => new RelayCommand(
        async execute => await RequestSidePanelViewSwitch(ViewCatalog.SidePanelView.SubNavMenu));

        protected async Task RequestMainContentViewSwitch(ViewCatalog.MainContentView requestedMainContentView)
        {
            var requestEvent = new RequestViewSwitchEvent(
                ViewCatalog.ContentPanel.MainContent, requestedMainContentView);
            await _events.PublishOnUIThreadAsync(requestEvent);
        }

        protected async Task RequestSidePanelViewSwitch(ViewCatalog.SidePanelView requestedSidePanelView)
        {
            var requestEvent = new RequestViewSwitchEvent(
                ViewCatalog.ContentPanel.SidePanel, requestedSidePanelView);
            await _events.PublishOnUIThreadAsync(requestEvent);
        }

        public virtual async Task HandleAsync(ViewSwitchedEvent message, CancellationToken cancellationToken)
        {
            switch (message.SwitchedContentPanel)
            {
                case ViewCatalog.ContentPanel.MainContent:
                    ActiveMainContentView = message.NewMainContentView;
                    break;
                case ViewCatalog.ContentPanel.SidePanel:
                    ActiveSidePanelView = message.NewSidePanelView;
                    break;
            }

            await Task.CompletedTask;
        }
    }
}
