using Caliburn.Micro;
using System.Threading;
using System.Threading.Tasks;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Utilities;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.SidePanel
{
    public class SubNavMenuViewModel : TaskViewModelBase
    {
        public bool IsProjectsSubMenu { get { return ActiveMainContentView == ViewCatalog.MainContentView.Projects; } }

        public string HeaderText { get { return IsProjectsSubMenu ? "PROJECTS" : "CONTEXTS"; } }


        public SubNavMenuViewModel(IEventAggregator events, IAppState appState, IWindowManager window,
                                   IDataState dataState, IDataService dataService, IDataHelper dataHelper) : base(events, appState, window, dataState, dataService, dataHelper)
        {
        }

        public override async Task HandleAsync(ViewSwitchedEvent message, CancellationToken cancellationToken)
        {
            await base.HandleAsync(message, cancellationToken);

            // header text may now be different
            NotifyOfPropertyChange(() => HeaderText);
        }
    }
}
