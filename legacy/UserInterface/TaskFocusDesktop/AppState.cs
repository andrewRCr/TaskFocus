using System;

namespace TaskFocusDesktop
{
    public delegate void AppStateChangedHandler(String propertyName, AppState state);

    public enum EPostSyncAction
    {
        None,
        Logout,
        Exit
    }

    public class AppState : IAppState
    {
        public event AppStateChangedHandler AppStateChanged = default!;

        private bool _shouldAutoLogin = true;
        public bool ShouldAutoLogin
        {
            get { return _shouldAutoLogin; }
            set
            {
                _shouldAutoLogin = value;
                AppStateChanged?.Invoke(nameof(ShouldAutoLogin), this);
            }
        }

        private bool _isAuthenticated;
        public bool IsAuthenticated
        {
            get { return _isAuthenticated; }
            set 
            { 
                _isAuthenticated = value;
                AppStateChanged?.Invoke(nameof(IsAuthenticated), this);
            }
        }

        private EPostSyncAction _pendingPostSyncAction = EPostSyncAction.None;
        public EPostSyncAction PendingPostSyncAction
        {
            get { return _pendingPostSyncAction; }
            set
            {
                _pendingPostSyncAction = value;
                AppStateChanged?.Invoke(nameof(PendingPostSyncAction), this);
            }
        }

        private string _alertMessage = string.Empty;
        public string AlertMessage
        {
            get { return _alertMessage; }
            set
            {
                _alertMessage = value;
                AppStateChanged?.Invoke(nameof(AlertMessage), this);
            }
        }

        private double _appWindowHeight;
        public double AppWindowHeight
        {
            get { return _appWindowHeight; }
            set 
            {
                _appWindowHeight = value;
                AppStateChanged?.Invoke(nameof(AppWindowHeight), this);
            }
        }

        private int _navMenuSelection = 1; // inbox selected by default
        public int NavMenuSelection
        {
            get => _navMenuSelection;
            set
            {
                _navMenuSelection = value;
                AppStateChanged?.Invoke(nameof(NavMenuSelection), this);
            }
        }
    }
}
