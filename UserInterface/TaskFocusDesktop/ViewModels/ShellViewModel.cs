using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.ViewModels.MainContent;
using TaskFocusDesktop.ViewModels.SidePanel;
using TaskFocusDesktop.ViewModels.TopPanel;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Models;
using static TaskFocusDesktop.AppState;

namespace TaskFocusDesktop.ViewModels
{
    public class ShellViewModel : Conductor<object>.Collection.AllActive, IHandle<LogOnEvent>, IHandle<LogOffEvent>, IHandle<MainContentViewSwitchEvent>
    {
        private IAPIHelper _apiHelper;
        private ILoggedInUserModel _loggedInUser;
        private IEventAggregator _events;
        private AppState _appState;

        private WindowState _shellWindowState;
        public WindowState ShellWindowState
        {
            get { return _shellWindowState;}
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
            get { return Borderless ? 0 : _outerMarginSize; }
            set { _outerMarginSize = value; }
        }

        // radius of the edges of the window
        private int _windowRadius = 10;
        public int WindowRadius
        {
            get { return Borderless ? 0 : _windowRadius; }
            set { _windowRadius = value; }
        }

        private Screen _topWidgetPanel;
        public Screen TopWidgetPanel
        {
            get { return _topWidgetPanel; }
            set
            {
                _topWidgetPanel = value;
                NotifyOfPropertyChange(() => TopWidgetPanel);
            }
        }

        private Screen _sideMenuPanel;
        public Screen SideMenuPanel
        {
            get { return _sideMenuPanel; }
            set
            {
                _sideMenuPanel = value;
                NotifyOfPropertyChange(() => SideMenuPanel);
            }
        }

        private Screen _mainContentPanel;
        public Screen MainContentPanel
        {
            get { return _mainContentPanel; }
            set
            {
                _mainContentPanel = value;
                NotifyOfPropertyChange(() => MainContentPanel);
            }
        }

        public AppState.MainContentView ActiveAppStateMainContentView { get { return _appState.ActiveMainContentView; } }

        private MainContentView _activeMainContentView;
        public MainContentView ActiveMainContentView
        {
            get { return _activeMainContentView; }
            set
            {
                _activeMainContentView = value;
                NotifyOfPropertyChange(() => ActiveMainContentView);
            }
        }

        public string WindowMaxRestoreIcon
        {
            get
            {
                return ShellWindowState == WindowState.Maximized ? "WindowRestore" : "WindowMaximize";
            }
        }

        public bool IsUserLoggedIn
        {
            get
            {
                return !string.IsNullOrWhiteSpace(_loggedInUser.Token);
            }
        }

        public double WindowMinimumWidth { get; set; } = 600;

        public double WindowMinimumHeight { get; set; } = 400;

        // true if the window should be borderless because it is docked or maximized
        public bool Borderless { get { return (ShellWindowState == WindowState.Maximized); } }

        public int TitleBarHeight { get; set; } = 26;

        public GridLength TitleBarHeightGridLength { get { return new GridLength(TitleBarHeight + ResizeBorder); } }

        public int ResizeBorder { get; set; } = 6;

        public Thickness ResizeBorderThickness { get { return new Thickness(ResizeBorder + OuterMarginSize); } }

        public Thickness OuterMarginSizeThickness { get { return new Thickness(OuterMarginSize); } }

        public CornerRadius WindowCornerRadius { get { return new CornerRadius(WindowRadius); } }

        public ICommand MinimizeCommand => new RelayCommand(execute => ShellWindowState = WindowState.Minimized);

        public ICommand MaximizeCommand => new RelayCommand(execute => ShellWindowState ^= WindowState.Maximized);

        public ICommand CloseCommand => new RelayCommand(async execute => await TryCloseAsync());

        public ICommand TitleBarMenuCommand => new RelayCommand(execute => SystemCommands.ShowSystemMenu(Application.Current.MainWindow, GetSystemMenuPosition()));

        //public ICommand SwitchToInboxViewCommand => new RelayCommand(async execute => await SwitchMainContentView(MainContentView.Inbox));

        //public ICommand SwitchToTodayViewCommand => new RelayCommand(async execute => await SwitchMainContentView(MainContentView.Today));

        //public ICommand SwitchToProjectsViewCommand => new RelayCommand(async execute => await SwitchMainContentView(MainContentView.Projects));

        //public ICommand SwitchToContextsViewCommand => new RelayCommand(async execute => await SwitchMainContentView(MainContentView.Contexts));

        //public ICommand SwitchToCompletedViewCommand => new RelayCommand(async execute => await SwitchMainContentView(MainContentView.Completed));

        public ICommand SwitchToSettingsViewCommand => new RelayCommand(async execute => await RequestSwitchToSettingsView());

        //public enum MainContentView
        //{
        //    Home,
        //    Inbox,
        //    Today,
        //    Projects,
        //    Contexts,
        //    Completed,
        //    Settings
        //}

        public ShellViewModel(IAPIHelper apiHelper,
                              ILoggedInUserModel loggedInUser,
                              IEventAggregator events,
                              AppState appState)
        {
            _apiHelper = apiHelper;
            _loggedInUser = loggedInUser;
            _events = events;
            _appState = appState;

            _events.SubscribeOnPublishedThread(this);

            // TODO: pull auth token from local storage

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
            //ActiveMainContentView = IsUserLoggedIn ? MainContentView.Inbox : MainContentView.Home;
            _appState.ActiveMainContentView = IsUserLoggedIn ? AppState.MainContentView.Inbox : AppState.MainContentView.Home;
        }

        //protected override bool HandleAppStateChanged(string propertyName, AppState appState)
        //{
        //    if (!appRefreshTriggers.Contains(propertyName)) { return false; }
        //    if (AppState.CanRefresh) { LoadAlertMessage(); }

        //    return AppState.CanRefresh;
        //}

        //private bool HandleAppStateChanged(string propertyName, AppState appState)
        //{
        //    if (!appRefreshTriggers.Contains(propertyName)) { return false; }
        //    if (appState.CanRefresh) { Async( SwitchMainContentView(); }

        //    return appState.CanRefresh;
        //}

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

        public async Task ExitApplication()
        {
            await TryCloseAsync();
        }

        public async Task HandleAsync(LogOnEvent message, CancellationToken cancellationToken)
        {
            await HandleLogIn();
        }

        public async Task HandleLogIn()
        {
            NotifyOfPropertyChange(() => IsUserLoggedIn);

            TopWidgetPanel = IoC.Get<AuthWidgetViewModel>();
            await ActivateItemAsync(TopWidgetPanel, new CancellationToken());

            MainContentPanel = IoC.Get<InboxViewModel>();
            await ActivateItemAsync(MainContentPanel, new CancellationToken());
            //ActiveMainContentView = MainContentView.Inbox;
            _appState.ActiveMainContentView = MainContentView.Inbox;
        }

        public async Task HandleAsync(LogOffEvent message, CancellationToken cancellationToken)
        {
            await HandleLogOut();
        }

        public async Task HandleLogOut()
        {
            NotifyOfPropertyChange(() => IsUserLoggedIn);

            TopWidgetPanel = IoC.Get<LoginWidgetViewModel>();
            await ActivateItemAsync(TopWidgetPanel, new CancellationToken());

            MainContentPanel = IoC.Get<HomeViewModel>();
            await ActivateItemAsync(MainContentPanel, new CancellationToken());
            //ActiveMainContentView = MainContentView.Home;
            _appState.ActiveMainContentView = MainContentView.Home;
        }

        public async Task HandleAsync(MainContentViewSwitchEvent message, CancellationToken cancellationToken)
        {
            await SwitchMainContentView();
        }

        public async Task SwitchMainContentView()
        {
            switch (_appState.ActiveMainContentView)
            {
                case AppState.MainContentView.Home:
                    MainContentPanel = IoC.Get<HomeViewModel>();
                    break;
                case AppState.MainContentView.Inbox:
                    MainContentPanel = IoC.Get<InboxViewModel>();
                    break;
                case AppState.MainContentView.Today:
                    MainContentPanel = IoC.Get<TodayViewModel>();
                    break;
                case AppState.MainContentView.Projects:
                    MainContentPanel = IoC.Get<ProjectsViewModel>();
                    break;
                case AppState.MainContentView.Contexts:
                    MainContentPanel = IoC.Get<ContextsViewModel>();
                    break;
                case AppState.MainContentView.Completed:
                    MainContentPanel = IoC.Get<CompletedViewModel>();
                    break;
                case AppState.MainContentView.Settings:
                    MainContentPanel = IoC.Get<SettingsViewModel>();
                    break;
                default:
                    MainContentPanel = IoC.Get<HomeViewModel>();
                    break;
            }

            await ActivateItemAsync(MainContentPanel, new CancellationToken());
            //ActiveMainContentView = mainContentView;
        }

        private async Task RequestSwitchToSettingsView()
        {
            ActiveMainContentView = AppState.MainContentView.Settings;
            NotifyOfPropertyChange(()=> ActiveAppStateMainContentView);
            await SwitchMainContentView();
        }
    }
}
