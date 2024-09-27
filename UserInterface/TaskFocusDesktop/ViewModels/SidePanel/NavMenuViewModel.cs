using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Utilities;
using TaskFocusDesktop.ViewModels.Base;

namespace TaskFocusDesktop.ViewModels.SidePanel
{
    public class NavMenuViewModel : ViewModelBase
    {
        public NavMenuViewModel(IEventAggregator events, IAppState appState) : base(events, appState)
        {
        }
    }
}
