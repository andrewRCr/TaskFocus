using Caliburn.Micro;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
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
        private IWindowManager _windowManager;
        private IAPIHelper _apiHelper;
        private ILoggedInUserModel _loggedInUser;
        private IEventAggregator _events;

        // last known dock position
        //private DockPosition _shellDockPosition;
        //public DockPosition ShellDockPosition
        //{
        //    get { return _shellDockPosition; }
        //    set
        //    {
        //        _shellDockPosition = value;
        //        NotifyOfPropertyChange(() => ShellDockPosition);
        //    }
        //}

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
            }
        }

        /// true if the window should be borderless because it is docked or maximized
        public bool Borderless { get { return (ShellWindowState == WindowState.Maximized); } }

        public int TitleBarHeight { get; set; } = 26;

        public GridLength TitleBarHeightGridLength { get { return new GridLength(TitleBarHeight + ResizeBorder); } }

        public int ResizeBorder { get; set; } = 6;

        public Thickness ResizeBorderThickness { get { return new Thickness(ResizeBorder + OuterMarginSize); } }

        public Thickness OuterMarginSizeThickness { get { return new Thickness(OuterMarginSize); } }

        public CornerRadius WindowCornerRadius { get { return new CornerRadius(WindowRadius); } }

        public ICommand MinimizeCommand => new RelayCommand(execute => ShellWindowState = WindowState.Minimized);

        public ICommand MaximizeCommand => new RelayCommand(execute => ShellWindowState = WindowState.Maximized);

        public ICommand CloseCommand => new RelayCommand(async execute => await TryCloseAsync());

        // margin around window, to allow a drop shadow
        private int _outerMarginSize = 10;
        public int OuterMarginSize
        {
            get { return Borderless ? 5 : _outerMarginSize; }
            set { _outerMarginSize = value; }
        }

        // radius of the edges of the window
        private int _windowRadius = 10;
        public int WindowRadius
        {
            get { return Borderless ? 5 : _windowRadius; }
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

        public ShellViewModel(IWindowManager windowManager,
                              IAPIHelper apiHelper,
                              ILoggedInUserModel loggedInUser,
                              IEventAggregator events)
        {
            _windowManager = windowManager;
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

            TopWidgetPanel = IoC.Get<LoginWidgetViewModel>();
            await ActivateItemAsync(TopWidgetPanel, new CancellationToken());

            MainContentPanel = IoC.Get<HomeViewModel>();
            await ActivateItemAsync(MainContentPanel, new CancellationToken());
            
            NotifyOfPropertyChange(() => IsUserLoggedIn);
        }

        public async Task HandleAsync(LogOnEvent message, CancellationToken cancellationToken)
        {
            NotifyOfPropertyChange(() => IsUserLoggedIn);

            TopWidgetPanel = IoC.Get<AuthWidgetViewModel>();
            await ActivateItemAsync(TopWidgetPanel, new CancellationToken());

            MainContentPanel = IoC.Get<InboxViewModel>();
            await ActivateItemAsync(MainContentPanel, new CancellationToken());
        }

        public async Task SwitchToInboxView()
        {
            MainContentPanel = IoC.Get<InboxViewModel>();
            await ActivateItemAsync(MainContentPanel, new CancellationToken());
        }

        public async Task SwitchToTodayView() 
        {
            MainContentPanel = IoC.Get<TodayViewModel>();
            await ActivateItemAsync(MainContentPanel, new CancellationToken());
        }
    }
}
