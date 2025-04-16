using Caliburn.Micro;
using TaskFocusDesktop.ViewModels.Base;

namespace TaskFocusDesktop.ViewModels.SidePanel
{
    public class NavMenuViewModel : ViewModelBase
    {
        public NavMenuViewModel(IEventAggregator events, IAppState appState) : base(events, appState) 
        {
            NavMenuSelection = appState.NavMenuSelection;
        }

        private int _navMenuSelection;
        public int NavMenuSelection
        {
            get => _navMenuSelection;
            set
            {
                _navMenuSelection = value;
                NotifyOfPropertyChange(() => NavMenuSelection);
            }
        }
    }
}
