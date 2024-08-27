using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFocusDesktop.EventModels
{
    public class FocusedProjectChangedEvent
    {
        public int NewFocusedProjectId { get; set; }

        public FocusedProjectChangedEvent(int newFocusedProjectId)
        {
            NewFocusedProjectId = newFocusedProjectId;
        }
    }
}
