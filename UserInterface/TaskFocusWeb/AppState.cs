using MudBlazor;

namespace TaskFocusWeb
{
    public delegate void AppStateChangedHandler(String propertyName, AppState state);

    public class AppState
    {
        public event AppStateChangedHandler AppStateChanged = default!;

        private MudBlazor.Severity _alertSeverity = MudBlazor.Severity.Normal;
        public MudBlazor.Severity AlertSeverity
        {
            get { return _alertSeverity; }
            set
            {
                _alertSeverity = value;
                AppStateChanged?.Invoke(nameof(AlertSeverity), this);
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

        private bool _canRefresh = true;
        public bool CanRefresh
        {
            get { return _canRefresh; }
            set
            {
                _canRefresh = value;
                AppStateChanged?.Invoke(nameof(CanRefresh), this);
            }
        }

        private bool _showProjectSubMenu = false;
        public bool ShowProjectSubMenu
        {
            get { return _showProjectSubMenu; }
            set
            {
                _showProjectSubMenu = value;
                AppStateChanged?.Invoke(nameof(ShowProjectSubMenu), this);
            }
        }

        private bool _showContextSubMenu = false;
        public bool ShowContextSubMenu
        {
            get { return _showContextSubMenu; }
            set
            {
                _showContextSubMenu = value;
                AppStateChanged?.Invoke(nameof(ShowContextSubMenu), this);
            }
        }

        private string? _focusedProjectIdStr = null;
        public string? FocusedProjectIdStr 
        {
            get { return _focusedProjectIdStr; } 
            set
            {
                _focusedProjectIdStr = value;
                AppStateChanged?.Invoke(nameof(FocusedProjectIdStr), this);
            }
        }

        private string? _focusedContextIdStr = null;
        public string? FocusedContextIdStr
        {
            get { return _focusedContextIdStr; }
            set
            {
                _focusedContextIdStr = value;
                AppStateChanged?.Invoke(nameof(FocusedContextIdStr), this);
            }
        }

        public void ClearAlertMessage()
        {
            _alertSeverity = MudBlazor.Severity.Info;
            _alertMessage = string.Empty;
        }

        public void ShowGenericLoginError()
        {
            _alertSeverity = MudBlazor.Severity.Error;
            AlertMessage = "There was an error when attempting to log in. Please try again.";
        }

        private MudDialogProvider _dialogProvider;
        public MudDialogProvider DialogProvider
        {
            get { return _dialogProvider; }
            set { _dialogProvider = value; }
        }
    }
}
