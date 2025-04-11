using Caliburn.Micro;
using TaskFocusDesktop.ViewModels.Base;

namespace TaskFocusDesktop.ViewModels.SidePanel
{
    public class NavMenuViewModel : ViewModelBase
    {
        public NavMenuViewModel(IEventAggregator events, IAppState appState) : base(events, appState) {}
    }
}
