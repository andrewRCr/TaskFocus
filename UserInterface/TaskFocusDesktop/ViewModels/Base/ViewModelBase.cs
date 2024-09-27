using Caliburn.Micro;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Utilities;


namespace TaskFocusDesktop.ViewModels.Base
{
    public abstract class ViewModelBase : Screen, IHandle<ViewSwitchedEvent>, INotifyPropertyChanged
    {
        protected IEventAggregator _events;
        protected IAppState _appState;
        protected ILog _logger = LogManager.GetLog(typeof(ViewModelBase));

        protected ViewModelBase(IEventAggregator events, IAppState appState)
        {
            _events = events;
            _events.SubscribeOnPublishedThread(this);
            _appState = appState;
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

        public ICommand SwitchToProjectSubNavMenuCommand => new RelayCommand(
                async execute => await RequestSidePanelViewSwitch(ViewCatalog.SidePanelView.ProjectSubNavMenu));

        public ICommand SwitchToContextSubNavMenuCommand => new RelayCommand(
                async execute => await RequestSidePanelViewSwitch(ViewCatalog.SidePanelView.ContextSubNavMenu));

        public ICommand ExitSubNavMenuCommand => new RelayCommand(async execute => await RequestExitSubNavMenu());

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

        protected async Task RequestExitSubNavMenu()
        {
            await RequestSidePanelViewSwitch(ViewCatalog.SidePanelView.NavMenu);
            await RequestMainContentViewSwitch(ViewCatalog.MainContentView.Home);
        }
    }
}
