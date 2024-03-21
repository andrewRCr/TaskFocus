using AutoMapper;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Models;

namespace TaskFocusWeb
{
    public delegate void AppStateChangedHandler(String propertyName, AppState state);

    public class AppState
    {
        public event AppStateChangedHandler AppStateChanged = default!;

        private bool _canRefresh = true;
        public bool CanRefresh
        {
            get { return _canRefresh; }
            set
            {
                //Console.WriteLine($"AppState: CanRefresh changed to {value}!");
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
                //Console.WriteLine($"AppState: FocusedProjectIdStr changed to {value}!");
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
