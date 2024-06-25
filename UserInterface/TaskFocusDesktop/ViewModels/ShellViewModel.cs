using Caliburn.Micro;
using MudBlazor;
using MudBlazor.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.ViewModels.MainContent;
using TaskFocusDesktop.ViewModels.TopPanel;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.ViewModels
{
    public class ShellViewModel : Conductor<object>.Collection.AllActive, IHandle<LogOnEvent>
    {
        private IAPIHelper _apiHelper;
        private ILoggedInUserModel _loggedInUser;
        private IEventAggregator _events;

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

        public ICommand SwitchToInboxViewCommand => new RelayCommand(async execute => await SwitchToInboxView());

        public ICommand SwitchToTodayViewCommand => new RelayCommand(async execute => await SwitchToTodayView());

        public ICommand SwitchToProjectsViewCommand => new RelayCommand(async execute => await SwitchToProjectsView());

        public ICommand SwitchToContextsViewCommand => new RelayCommand(async execute => await SwitchToContextsView());

        public ICommand SwitchToCompletedViewCommand => new RelayCommand(async execute => await SwitchToCompletedView());

        public ICommand SwitchToSettingsViewCommand => new RelayCommand(async execute => await SwitchToSettingsView());

        public enum MainContentView
        {
            Home,
            Inbox,
            Today,
            Projects,
            Contexts,
            Completed,
            Settings
        }

        private MainContentView _activeMainContentView = MainContentView.Inbox;
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


        private MenuItem _inboxNavMenuItem;
        public MenuItem InboxNavMenuItem
        {
            get { return _inboxNavMenuItem; }
            set { _inboxNavMenuItem = value; }
        }

        private MenuItem _todayNavMenuItem;
        public MenuItem TodayNavMenuItem
        {
            get { return _todayNavMenuItem; }
            set { _todayNavMenuItem = value; }
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

        public ShellViewModel(IAPIHelper apiHelper,
                              ILoggedInUserModel loggedInUser,
                              IEventAggregator events)
        {
            _apiHelper = apiHelper;
            _loggedInUser = loggedInUser;
            _events = events;

            _events.SubscribeOnPublishedThread(this);

            // TODO: pull auth token from local storage

            // top widget panel
            TopWidgetPanel = IsUserLoggedIn ? IoC.Get<AuthWidgetViewModel>() : IoC.Get<LoginWidgetViewModel>();
            ActivateItemAsync(TopWidgetPanel, new CancellationToken());

            // main content panel
            MainContentPanel = IsUserLoggedIn ? IoC.Get<InboxViewModel>() : IoC.Get<HomeViewModel>();
            ActivateItemAsync(MainContentPanel, new CancellationToken());

            // set current main content view enum to default
            ActiveMainContentView = IsUserLoggedIn ? MainContentView.Inbox : MainContentView.Home;
        }

        public bool IsUserLoggedIn
        {
            get
            {
                return !string.IsNullOrWhiteSpace(_loggedInUser.Token);
            }
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

        public async Task ExitApplication()
        {
            await TryCloseAsync();
        }

        public async Task LogOut()
        {
            _apiHelper.LogOutUser();
            _loggedInUser.ResetUserModel();

            TopWidgetPanel = IoC.Get<LoginWidgetViewModel>();
            await ActivateItemAsync(TopWidgetPanel, new CancellationToken());

            MainContentPanel = IoC.Get<HomeViewModel>();
            await ActivateItemAsync(MainContentPanel, new CancellationToken());
            ActiveMainContentView = MainContentView.Home;

            NotifyOfPropertyChange(() => IsUserLoggedIn);
        }

        public async Task HandleAsync(LogOnEvent message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(() => IsUserLoggedIn);

            TopWidgetPanel = IoC.Get<AuthWidgetViewModel>();
            await ActivateItemAsync(TopWidgetPanel, new CancellationToken());

            MainContentPanel = IoC.Get<InboxViewModel>();
            await ActivateItemAsync(MainContentPanel, new CancellationToken());
            ActiveMainContentView = MainContentView.Inbox;
        }

        public async Task HandleAsync(LogOffEvent message, CancellationToken cancellationToken)
        {
            await LogOut();
        }

        public async Task SwitchToInboxView()
        {
            ActiveMainContentView = MainContentView.Inbox;

            MainContentPanel = IoC.Get<InboxViewModel>();
            await ActivateItemAsync(MainContentPanel, new CancellationToken());
        }

        public async Task SwitchToTodayView() 
        {
            ActiveMainContentView = MainContentView.Today;

            MainContentPanel = IoC.Get<TodayViewModel>();
            await ActivateItemAsync(MainContentPanel, new CancellationToken());
        }

        public async Task SwitchToProjectsView()
        {
            ActiveMainContentView = MainContentView.Projects;

            MainContentPanel = IoC.Get<ProjectsViewModel>();
            await ActivateItemAsync(MainContentPanel, new CancellationToken());
        }

        public async Task SwitchToContextsView()
        {
            ActiveMainContentView = MainContentView.Contexts;

            MainContentPanel = IoC.Get<ContextsViewModel>();
            await ActivateItemAsync(MainContentPanel, new CancellationToken());
        }

        public async Task SwitchToCompletedView()
        {
            ActiveMainContentView = MainContentView.Completed;

            MainContentPanel = IoC.Get<CompletedViewModel>();
            await ActivateItemAsync(MainContentPanel, new CancellationToken());
        }

        public async Task SwitchToSettingsView()
        {
            ActiveMainContentView = MainContentView.Settings;

            MainContentPanel = IoC.Get<SettingsViewModel>();
            await ActivateItemAsync(MainContentPanel, new CancellationToken());
        }
    }
}
