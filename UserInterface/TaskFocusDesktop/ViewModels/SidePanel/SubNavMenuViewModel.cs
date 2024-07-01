using Caliburn.Micro;
using System.Threading;
using System.Threading.Tasks;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Utilities;
using TaskFocusDesktop.ViewModels.Base;

namespace TaskFocusDesktop.ViewModels.SidePanel
{
    public class SubNavMenuViewModel : ViewModelBase
    {
        public bool IsProjectsSubMenu { get { return ActiveMainContentView == ViewCatalog.MainContentView.Projects; } }

        public string HeaderText { get { return IsProjectsSubMenu ? "PROJECTS" : "CONTEXTS"; } }

        public SubNavMenuViewModel(IEventAggregator events) : base(events)
        {
            _events = events;
        }

        public override async Task HandleAsync(ViewSwitchedEvent message, CancellationToken cancellationToken)
        {
            await base.HandleAsync(message, cancellationToken);

            // header text may now be different
            NotifyOfPropertyChange(() => HeaderText);
        }
    }
}
