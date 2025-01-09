using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFocusDesktop.EventModels
{
    class UnconfirmedEmailNotifyEvent
    {
        public string Email { get; set; }

        public UnconfirmedEmailNotifyEvent(string emailAddress)
        {
            Email = emailAddress;
        }
    }
}
