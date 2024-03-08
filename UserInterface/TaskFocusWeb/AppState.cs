namespace TaskFocusWeb
{
    public delegate void AppStateChangedHandler(String propertyName, AppState state);

    public class AppState
    {
        public event AppStateChangedHandler AppStateChanged = default!;

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
    }
}
