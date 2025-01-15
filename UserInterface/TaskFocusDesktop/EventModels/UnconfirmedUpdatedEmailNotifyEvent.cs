using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFocusDesktop.EventModels
{
    public class UnconfirmedUpdatedEmailNotifyEvent
    {
        public string Email { get; set; }

        public UnconfirmedUpdatedEmailNotifyEvent(string emailAddress)
        {          
            Email = emailAddress;
        }
    }
}
