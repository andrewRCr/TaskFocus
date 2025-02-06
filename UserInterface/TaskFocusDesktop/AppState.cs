using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFocusDesktop
{
    public delegate void AppStateChangedHandler(String propertyName, AppState state);

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
    }
}
