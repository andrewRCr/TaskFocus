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
    }
}
