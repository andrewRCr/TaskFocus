using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Utilities;
using TaskFocusDesktop.ViewModels.Dialogs;
using TaskFocusDesktop.ViewModels.MainContent;
using TaskFocusDesktop.ViewModels.SidePanel;
using TaskFocusDesktop.ViewModels.TopPanel;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.Services.Synchronization;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Data.Utilities;
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.ViewModels
{
    public class ShellViewModel : Conductor<object>.Collection.AllActive, 
                                  IHandle<AuthStatusChangedEvent>, 
                                  IHandle<RequestViewSwitchEvent>, 
                                  IHandle<RequestShowDialogEvent>, 
                                  IHandle<FocusedProjectChangedEvent>, 
                                  IHandle<FocusedContextChangedEvent>,
                                  IHandle<PostSyncActionRequestEvent>
    {
        private ILog _logger = LogManager.GetLog(typeof(ShellViewModel));
        private const string _dialogIdentifier = "ShellDialogHost";
        private string? _focusedProjectName;
        private int? _focusedProjectId;
        private string? _focusedContextName;
        private int? _focusedContextId;

        private IAPIHelper _apiHelper;
        private ILoggedInUserModel _loggedInUser;
        private IEventAggregator _events;
        protected IWindowManager _window;
        protected IDataService _dataService;
        protected IDataSyncService _dataSyncService;
        protected IDataHelper _dataHelper;
        protected IDataState _dataState;
        protected IAppState _appState;

        protected List<string> dataRefreshTriggers = new List<string> {
            nameof(EDataRefreshType.AppRequestedSyncCompleted), nameof(EDataRefreshType.SyncStatus) };

        public ShellViewModel(IAPIHelper apiHelper,
                      ILoggedInUserModel loggedInUser,
                      IEventAggregator events,
                      IDataService dataService,
                      IDataSyncService dataSyncService,
                      IDataHelper dataHelper,
                      IDataState dataState,
                      IAppState appState)
        {
            _apiHelper = apiHelper;
            _loggedInUser = loggedInUser;
            _events = events;
            _dataService = dataService;
            _dataSyncService = dataSyncService;
            _dataHelper = dataHelper;
            _dataState = dataState;
            _appState = appState;

            _events.SubscribeOnPublishedThread(this);
            _dataState.DataStateChanged += DataStateChanged;

            // top widget panel
            TopWidgetPanel = IsUserLoggedIn ? IoC.Get<AuthWidgetViewModel>() : IoC.Get<LoginWidgetViewModel>();
            ActivateItemAsync(TopWidgetPanel, new CancellationToken());

            // side menu panel
            SideMenuPanel = IoC.Get<NavMenuViewModel>();
            ActivateItemAsync(SideMenuPanel, new CancellationToken());

            // main content panel
            MainContentPanel = IsUserLoggedIn ? IoC.Get<InboxViewModel>() : IoC.Get<HomeViewModel>();
            ActivateItemAsync(MainContentPanel, new CancellationToken());

            // set current main content view enum to default
            ActiveMainContentView = IsUserLoggedIn ? ViewCatalog.MainContentView.Inbox : ViewCatalog.MainContentView.Home;

            UpdateMiniNavIconColor();
            UpdateSettingsMenuItemColor();
        }

        // app appearance props
        // ====================

        public double WindowMinimumWidth { get; set; } = 600;
        public double WindowMinimumHeight { get; set; } = 400;
        public bool Borderless { get => (ShellWindowState == WindowState.Maximized); }
        public int TitleBarHeight { get; set; } = 26;
        public GridLength TitleBarHeightGridLength { get => new GridLength(TitleBarHeight + ResizeBorder); }
        public int ResizeBorder { get; set; } = 6;
        public Thickness ResizeBorderThickness { get => new Thickness(ResizeBorder + OuterMarginSize); }
        public Thickness OuterMarginSizeThickness { get => new Thickness(OuterMarginSize); }
        public CornerRadius WindowCornerRadius { get => new CornerRadius(WindowRadius); }

        private WindowState _shellWindowState;
        public WindowState ShellWindowState
        {
            get => _shellWindowState;
            set 
            { 
                _shellWindowState = value;
                NotifyOfPropertyChange(() => ShellWindowState);
                NotifyOfPropertyChange(() => ResizeBorderThickness);
                NotifyOfPropertyChange(() => OuterMarginSize);
                NotifyOfPropertyChange(() => OuterMarginSizeThickness);
                NotifyOfPropertyChange(() => WindowRadius);
                NotifyOfPropertyChange(() => WindowCornerRadius);
                NotifyOfPropertyChange(() => WindowMaxRestoreIcon);
            }
        }

        // margin around window, to allow a drop shadow
        private int _outerMarginSize = 10;
        public int OuterMarginSize
        {
            get => Borderless ? 0 : _outerMarginSize;
            set => _outerMarginSize = value;
        }

        // radius of the edges of the window
        private int _windowRadius = 10;
        public int WindowRadius
        {
            get => Borderless ? 0 : _windowRadius;
            set => _windowRadius = value;
        }

        private SolidColorBrush _miniNavIconColor;
        public SolidColorBrush MiniNavIconColor
        {
            get => _miniNavIconColor;
            set 
            { 
                _miniNavIconColor = value;
                NotifyOfPropertyChange(() => MiniNavIconColor);
            }
        }

        private SolidColorBrush _settingsMenuItemColor;
        public SolidColorBrush SettingsMenuItemColor
        {
            get => _settingsMenuItemColor;
            set 
            { 
                _settingsMenuItemColor = value;
                NotifyOfPropertyChange(() => SettingsMenuItemColor);
            }
        }

        public string WindowMaxRestoreIcon
        {
            get  => ShellWindowState == WindowState.Maximized ? "WindowRestore" : "WindowMaximize";       
        }

        // app behavior props
        // ====================

        public bool IsUserLoggedIn
        {
            get => !string.IsNullOrWhiteSpace(_loggedInUser.Token);        
        }

        private Screen? _topWidgetPanel;
        public Screen? TopWidgetPanel
        {
            get => _topWidgetPanel;
            set
            {
                _topWidgetPanel = value;
                NotifyOfPropertyChange(() => TopWidgetPanel);
            }
        }

        private Screen? _sideMenuPanel;
        public Screen? SideMenuPanel
        {
            get => _sideMenuPanel;
            set
            {
                _sideMenuPanel = value;
                NotifyOfPropertyChange(() => SideMenuPanel);
            }
        }

        private Screen? _mainContentPanel;
        public Screen? MainContentPanel
        {
            get => _mainContentPanel;
            set
            {
                _mainContentPanel = value;
                NotifyOfPropertyChange(() => MainContentPanel);
            }
        }

        private ViewCatalog.MainContentView _activeMainContentView;
        public ViewCatalog.MainContentView ActiveMainContentView
        {
            get => _activeMainContentView;
            set
            {
                _activeMainContentView = value;
                NotifyOfPropertyChange(() => ActiveMainContentView);
            }
        }

        private ViewCatalog.SidePanelView _activeSidePanelView;
        public ViewCatalog.SidePanelView ActiveSidePanelView
        {
            get => _activeSidePanelView;            
            set
            {
                _activeSidePanelView = value;
                NotifyOfPropertyChange(() => ActiveSidePanelView);
            }
        }

        private string _syncStatusStr = "Initializing...";
        public string SyncStatusStr
        {
            get => _syncStatusStr;
            set
            {
                _syncStatusStr = value;
                NotifyOfPropertyChange(() => SyncStatusStr);
            }
        }

        private SolidColorBrush _syncStatusColor;
        public SolidColorBrush SyncStatusColor
        {
            get => _syncStatusColor;
            set
            {
                _syncStatusColor = value;
                NotifyOfPropertyChange(() => SyncStatusColor);
            }
        }

        public ICommand MinimizeCommand => new RelayCommand(execute => ShellWindowState = WindowState.Minimized);
        public ICommand MaximizeCommand => new RelayCommand(execute => ShellWindowState ^= WindowState.Maximized);
        public ICommand CloseCommand => new RelayCommand(execute => ExitApplication());
        public ICommand TitleBarMenuCommand => new RelayCommand(execute => SystemCommands.ShowSystemMenu(Application.Current.MainWindow, GetSystemMenuPosition()));

        public ICommand SwitchToInboxViewCommand => new RelayCommand(async execute => await SwitchMainContentView(ViewCatalog.MainContentView.Inbox));
        public ICommand SwitchToTodayViewCommand => new RelayCommand(async execute => await SwitchMainContentView(ViewCatalog.MainContentView.Today));
        public ICommand SwitchToProjectsViewCommand => new RelayCommand(async execute => await SwitchMainContentView(ViewCatalog.MainContentView.Projects));
        public ICommand SwitchToContextsViewCommand => new RelayCommand(async execute => await SwitchMainContentView(ViewCatalog.MainContentView.Contexts));
        public ICommand SwitchToCompletedViewCommand => new RelayCommand(async execute => await SwitchMainContentView(ViewCatalog.MainContentView.Completed));
        public ICommand SwitchToSettingsViewCommand => new RelayCommand(async execute => await SwitchMainContentView(ViewCatalog.MainContentView.Settings));

        public ICommand OpenNewTaskDialogCommand => new RelayCommand(async execute => await ShowDialog(ViewCatalog.DialogView.AddNewTaskDialog));

        // app appearance methods
        // ====================

        private void AppWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Window appWindow = (Window)GetView();
            double newHeight = appWindow.Height;

            _appState.AppWindowHeight = newHeight;
            _events.PublishOnUIThreadAsync(new AppWindowHeightChangedEvent(newHeight));
        }

        private Point GetSystemMenuPosition()
        {
            Window appWindow = (Window)GetView();

            // position of the mouse relative to the window
            var position = Mouse.GetPosition(appWindow);

            // ensure correct position when maximized on non-primary monitors
            var currentScreen = WpfScreenHelper.Screen.FromWindow(appWindow);

            // add the current screen's position so it's relative to that screen
            Point maximizedPosition = new Point(
                position.X + currentScreen.WpfWorkingArea.Left,
                position.Y + currentScreen.WpfWorkingArea.Top);

            // add the app window position so it's relative to the screen
            Point normalPosition = new Point(position.X + appWindow.Left, position.Y + appWindow.Top);

            return ShellWindowState == WindowState.Maximized ? maximizedPosition : normalPosition;
        }

        private void UpdateMiniNavIconColor()
        {
            string hexValue = IsUserLoggedIn ? "#c2c2c5" : "#737379"; // foreground main/tertiary 939397
            MiniNavIconColor = (SolidColorBrush)new BrushConverter().ConvertFrom(hexValue)!;
        }

        private void UpdateSettingsMenuItemColor()
        {
            string hexValue = ActiveMainContentView == ViewCatalog.MainContentView.Settings ? "#776be7" : "#c2c2c5"; // foreground highlight/main
            SettingsMenuItemColor = (SolidColorBrush)new BrushConverter().ConvertFrom(hexValue)!;
        }

        private void UpdateSyncStatusColor()
        {
            string hexValue = SyncStatusStr == "Synchronized" ? "#776be7" : "#c2c2c5"; // foreground highlight/main
            SyncStatusColor = (SolidColorBrush)new BrushConverter().ConvertFrom(hexValue)!;
        }

        private string GetSyncStatusString()
        {
            string statusStr = "?";
            switch (_dataService.GetCurrentSyncStatus())
            {
                case (ESyncStatus.Initializing):
                    statusStr = "Initializing...";
                    break;

                case (ESyncStatus.Synchronized):
                    statusStr = "Synchronized";
                    break;

                case (ESyncStatus.SyncInProgress):
                    statusStr = "Syncing...";
                    break;

                default:
                    break;
            }

            return statusStr;
        }

        // app behavior methods
        // ====================

        private void ExitApplication()
        {
            RequestSyncAndPostActionEvent(EPostSyncAction.Exit);
        }

        private void DataStateChanged(String propertyName, IDataState dataState)
        {
            bool changesOccured = HandleDataStateChanged(propertyName, dataState);
            if (changesOccured)
            {
                //_logger.Info($"ShellViewModel: returned true on HandleDataStateChanged. changed property causing True return: {propertyName}");
            }
        }

        // on state refresh, check for a requested post-sync action and fire event to fulfill if needed
        protected bool HandleDataStateChanged(string propertyName, IDataState dataState)
        {
            if (!dataRefreshTriggers.Contains(propertyName)) return false;
            else if (_appState.PendingPostSyncAction != EPostSyncAction.None)
            {
                _events.PublishOnUIThreadAsync(new PostSyncActionRequestEvent(_appState.PendingPostSyncAction));
                _appState.PendingPostSyncAction = EPostSyncAction.None; // reset
            }

            SyncStatusStr = GetSyncStatusString();
            UpdateSyncStatusColor();

            return true;
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            Window appWindow = (Window)GetView();
            _appState.AppWindowHeight = appWindow.Height;
            appWindow.SizeChanged += AppWindow_SizeChanged;
        }

        // AuthStatusChangedEvent handler
        public async Task HandleAsync(AuthStatusChangedEvent message, CancellationToken cancellationToken)
        {
            bool authenticated = message.NewAuthStatus;
            if (authenticated) await HandleLogIn();
            else { RequestSyncAndPostActionEvent(EPostSyncAction.Logout); }
        }

        public async Task HandleLogIn()
        {
            NotifyOfPropertyChange(() => IsUserLoggedIn);
            _appState.IsAuthenticated = true;
            UpdateMiniNavIconColor();

            // if DataState has no remote data loaded, load it
            if (!_dataService.IsDataStateLoaded())
            {
                _dataService.InvokeSyncRequest($"{this.ToString()}: {nameof(HandleLogIn)}");
            }

            GetSyncStatusString();
            UpdateSyncStatusColor();

            TopWidgetPanel = IoC.Get<AuthWidgetViewModel>();
            await ActivateItemAsync(TopWidgetPanel, new CancellationToken());

            MainContentPanel = IoC.Get<InboxViewModel>();
            await ActivateItemAsync(MainContentPanel, new CancellationToken());
            ActiveMainContentView = ViewCatalog.MainContentView.Inbox;
        }

        public void RequestSyncAndPostActionEvent(EPostSyncAction action)
        {
            _appState.PendingPostSyncAction = action;
            _dataService.InvokeSyncRequest($"{this.ToString()}: {nameof(RequestSyncAndPostActionEvent)}", true);
        }

        // PostSyncActionRequestEvent handler
        public async Task HandleAsync(PostSyncActionRequestEvent message, CancellationToken cancellationToken)
        {
            switch (message.RequestedAction)
            {
                case EPostSyncAction.Logout:

                    _apiHelper.LogOutUser();
                    NotifyOfPropertyChange(() => IsUserLoggedIn);
                    _appState.IsAuthenticated = false;
                    _appState.ShouldAutoLogin = false;
                    UpdateMiniNavIconColor();

                    TopWidgetPanel = IoC.Get<LoginWidgetViewModel>();
                    await ActivateItemAsync(TopWidgetPanel, new CancellationToken());

                    MainContentPanel = IoC.Get<HomeViewModel>();
                    await ActivateItemAsync(MainContentPanel, new CancellationToken());
                    ActiveMainContentView = ViewCatalog.MainContentView.Home;

                    _dataService.ResetDataStateOnLogout();

                    break;

                case EPostSyncAction.Exit:

                    await TryCloseAsync();
                    break;

                default:
                    break;
            }
        }

        // user-invoked manual sync request
        public void ManualSync()
        {
            _dataService.InvokeSyncRequest($"{this.ToString()}: {nameof(ManualSync)}");
        }

        // RequestViewSwitchEvent handler
        public async Task HandleAsync(RequestViewSwitchEvent message, CancellationToken cancellationToken)
        {
            switch (message.RequestedContentPanel)
            {
                case ViewCatalog.ContentPanel.MainContent:
                    await SwitchMainContentView(message.RequestedMainContentView);
                    break;

                case ViewCatalog.ContentPanel.SidePanel:
                    await SwitchSidePanelView(message.RequestedSidePanelView);
                    break;

                case ViewCatalog.ContentPanel.TopPanel:
                    break;
            }
        }

        public async Task SwitchMainContentView(ViewCatalog.MainContentView requestedMainContentView)
        {
            switch (requestedMainContentView)
            {
                case ViewCatalog.MainContentView.Home:
                    MainContentPanel = IoC.Get<HomeViewModel>();
                    break;
                case ViewCatalog.MainContentView.Inbox:
                    MainContentPanel = IoC.Get<InboxViewModel>();
                    break;
                case ViewCatalog.MainContentView.Today:
                    MainContentPanel = IoC.Get<TodayViewModel>();
                    break;
                case ViewCatalog.MainContentView.Projects:
                    MainContentPanel = IoC.Get<ProjectsViewModel>();
                    break;
                case ViewCatalog.MainContentView.Contexts:
                    MainContentPanel = IoC.Get<ContextsViewModel>();
                    break;
                case ViewCatalog.MainContentView.Completed:
                    MainContentPanel = IoC.Get<CompletedViewModel>();
                    break;
                case ViewCatalog.MainContentView.Settings:
                    MainContentPanel = IoC.Get<SettingsViewModel>();
                    break;
                default:
                    MainContentPanel = IoC.Get<HomeViewModel>();
                    break;
            }

            // activate appropriate side panel view first so it can consume ViewSwitchedEvent
            if (requestedMainContentView == ViewCatalog.MainContentView.Projects)
            {
                await SwitchSidePanelView(ViewCatalog.SidePanelView.ProjectSubNavMenu);
            }
            else if (requestedMainContentView == ViewCatalog.MainContentView.Contexts)
            {
                await SwitchSidePanelView(ViewCatalog.SidePanelView.ContextSubNavMenu);
            }
            else if (ActiveSidePanelView != ViewCatalog.SidePanelView.NavMenu)
            {
                await SwitchSidePanelView(ViewCatalog.SidePanelView.NavMenu);
            }

            await ActivateItemAsync(MainContentPanel, new CancellationToken());
            ActiveMainContentView = requestedMainContentView;

            // notify other views
            var switchedEvent = new ViewSwitchedEvent(ViewCatalog.ContentPanel.MainContent, ActiveMainContentView);
            await _events.PublishOnUIThreadAsync(switchedEvent);

            // handle settings menu item (as it exists outside NavMenu)
            UpdateSettingsMenuItemColor();
        }

        public async Task SwitchSidePanelView(ViewCatalog.SidePanelView requestedSidePanelView)
        {
            // clear UI collection focus record, for NewTask dialogs
            _focusedProjectName = null;
            _focusedContextName = null;

            switch (requestedSidePanelView)
            {
                case ViewCatalog.SidePanelView.NavMenu:
                    SideMenuPanel = IoC.Get<NavMenuViewModel>();
                    break;
                case ViewCatalog.SidePanelView.ProjectSubNavMenu:
                    SideMenuPanel = IoC.Get<ProjectSubNavMenuViewModel>();
                    break;
                case ViewCatalog.SidePanelView.ContextSubNavMenu:
                    SideMenuPanel = IoC.Get<ContextSubNavMenuViewModel>();
                    break;
                default:
                    SideMenuPanel = IoC.Get<NavMenuViewModel>();
                    break;
            }

            await ActivateItemAsync(SideMenuPanel, new CancellationToken());
            ActiveSidePanelView = requestedSidePanelView;

            // notify other views
            var switchedEvent = new ViewSwitchedEvent(ViewCatalog.ContentPanel.SidePanel, ActiveSidePanelView);
            await _events.PublishOnUIThreadAsync(switchedEvent);
        }

        // RequestShowDialogEvent handler
        public async Task HandleAsync(RequestShowDialogEvent message, CancellationToken cancellationToken)
        {
            await ShowDialog(message.RequestedDialogView);
        }

        public async Task ShowDialog(ViewCatalog.DialogView requestedDialogView)
        {
            object? dialogVM = null;
            IWindowManager dummyWindow = new WindowManager();
            object? extendedDialogVM = null;

            switch (requestedDialogView)
            {
                case ViewCatalog.DialogView.AddNewProjectDialog:
                    dialogVM = new NewProjectDialogViewModel(_events, _appState, dummyWindow, _dataState, _dataService, _dataHelper);
                    break;

                case ViewCatalog.DialogView.AddNewContextDialog:
                    dialogVM = new NewContextDialogViewModel(_events, _appState, dummyWindow, _dataState, _dataService, _dataHelper);
                    break;

                case ViewCatalog.DialogView.AddNewTaskDialog:
                    extendedDialogVM = new NewTaskDialogViewModel(
                        _events, _appState, dummyWindow, _dataState, _dataService, _dataHelper, _focusedProjectName, _focusedContextName);
                    break;

                case ViewCatalog.DialogView.RenameProjectDialog:
                    if (_focusedProjectId != null && _focusedProjectName != null)
                    {
                        extendedDialogVM = new RenameCollectionDialogViewModel(_events, _appState, dummyWindow, _dataState, _dataService, 
                            _dataHelper, true, (int)_focusedProjectId, _focusedProjectName);
                    }
                    break;

                case ViewCatalog.DialogView.RenameContextDialog:
                    if (_focusedContextId != null && _focusedContextName != null)
                    {
                        extendedDialogVM = new RenameCollectionDialogViewModel(_events, _appState, dummyWindow, _dataState, _dataService, 
                            _dataHelper, false, (int)_focusedContextId, _focusedContextName);
                    }
                    break;

                case ViewCatalog.DialogView.DeleteProjectDialog:
                    if (_focusedProjectId != null && _focusedProjectName != null)
                    {
                        extendedDialogVM = new DeleteCollectionDialogViewModel( _events, _appState, dummyWindow, _dataState, _dataService, 
                            _dataHelper, true, (int)_focusedProjectId, _focusedProjectName);
                    }
                    break;

                case ViewCatalog.DialogView.DeleteContextDialog:
                    if (_focusedContextId != null && _focusedContextName != null)
                    {
                        extendedDialogVM = new DeleteCollectionDialogViewModel( _events, _appState, dummyWindow, _dataState, _dataService, 
                            _dataHelper, false, (int)_focusedContextId, _focusedContextName);
                    }
                    break;

                case ViewCatalog.DialogView.UpdateEmailDialog:
                    dialogVM = new UpdateEmailDialogViewModel(
                        _events, _appState, dummyWindow, _dataState, _dataService, _dataHelper, _apiHelper, _loggedInUser);
                    break;

                case ViewCatalog.DialogView.ChangePasswordDialog:
                   extendedDialogVM = new ChangePasswordDialogViewModel(
                        _events, _appState, dummyWindow, _dataState, _dataService, _dataHelper, _apiHelper, _loggedInUser);
                    break;

                default:
                    break;
            }

            if (dialogVM != null) { await DialogHost.Show(dialogVM, _dialogIdentifier); }
            else if (extendedDialogVM != null) // non-templated dialogs w/ additional data requirements
            {
                UIElement uiElement = ViewLocator.LocateForModel(extendedDialogVM, null, null);
                ViewModelBinder.Bind(extendedDialogVM, uiElement, null);
                await DialogHost.Show(uiElement, _dialogIdentifier);
            }
        }

        // FocusedProjectChangedEvent handler
        public Task HandleAsync(FocusedProjectChangedEvent message, CancellationToken cancellationToken)
        {
            _focusedProjectName = message.NewFocusedProjectName;
            _focusedProjectId = message.NewFocusedProjectId;
            return Task.CompletedTask;
        }

        // FocusedContextChangedEvent handler
        public Task HandleAsync(FocusedContextChangedEvent message, CancellationToken cancellationToken)
        {
            _focusedContextName = message.NewFocusedContextName;
            _focusedContextId = message.NewFocusedContextId;
            return Task.CompletedTask;
        }
    }
}
