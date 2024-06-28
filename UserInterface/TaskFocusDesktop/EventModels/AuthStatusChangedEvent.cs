using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFocusDesktop.EventModels
{
    public class AuthStatusChangedEvent
    {
        public bool NewAuthStatus { get; set; }

        public AuthStatusChangedEvent(bool newAuthStatus)
        {
            NewAuthStatus = newAuthStatus; 
        }
    }
}
