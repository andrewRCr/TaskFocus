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

        //public IObservableCollection<ProjectDisplayModel>? Projects { get; set; }

        //public IObservableCollection<ContextDisplayModel>? Contexts { get; set; }

        public SubNavMenuViewModel(IEventAggregator events,
                                   IWindowManager window,
                                   IDataState dataState,
                                   IDataService dataService,
                                   IDataHelper dataHelper) : base(events, window, dataState, dataService, dataHelper)
        {
            //_events = events;
        }

        public override async Task HandleAsync(ViewSwitchedEvent message, CancellationToken cancellationToken)
        {
            await base.HandleAsync(message, cancellationToken);

            // header text may now be different
            NotifyOfPropertyChange(() => HeaderText);
        }


    }
}
