using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFocusDesktop.EventModels
{
    public class AppWindowHeightChangedEvent
    {
        public double NewAppWindowHeight { get; set; }

        public AppWindowHeightChangedEvent(double newAppWindowHeight)
        {
            NewAppWindowHeight = newAppWindowHeight;
        }
    }
}
