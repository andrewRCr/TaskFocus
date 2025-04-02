using Caliburn.Micro;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Data.Utilities;
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.ViewModels.MainContent
{
    public class InboxViewModel : TaskViewModelBase, INotifyPropertyChanged
    {
        public InboxViewModel(IEventAggregator events,
                              IAppState appState,
                              IWindowManager window,
                              IDataState dataState,
                              IDataService dataService,
                              IDataHelper dataHelper) : base(events, appState, window, dataState, dataService, dataHelper)
        {
            OrderingIndex = "InboxIndex";
        }

        private bool _showEmptyTaskListTutorialText = false;
        public bool ShowEmptyTaskListTutorialText
        {
            get { return _showEmptyTaskListTutorialText; }
            set
            {
                _showEmptyTaskListTutorialText = value;
                NotifyOfPropertyChange(() => ShowEmptyTaskListTutorialText);
            }
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            ActiveMainContentView = Utilities.ViewCatalog.MainContentView.Inbox;
        }

        protected override void LoadLocalTaskData()
        {
            if (_dataState.IsDataLoaded())
            {
                List<TaskDisplayModel> inboxTasks = _dataState.Tasks!.Where(x => (x.ProjectId == null || x.ContextId == null) && !x.CleanedUp).ToList();
                var orderedInboxTasks = inboxTasks.OrderBy(x => x.InboxIndex);
                inboxTasks = orderedInboxTasks.ToList();
                LocalTasks = new ObservableCollection<TaskDisplayModel>(inboxTasks);

                foreach (TaskDisplayModel task in LocalTasks!)
                {
                    task.PropertyChanged += OnExistingTaskPropertyChanged!; // subscribe to property changed event
                }

                ShowEmptyTaskListTutorialText = LocalTasks.Count == 0;

                TaskCount = LocalTasks.Count;
                UpdateScrollHeight(AppWindowHeight);
            }
        }

        protected override bool HandleDataStateChanged(string propertyName, IDataState dataState)
        {
            if (!dataRefreshTriggers.Contains(propertyName) || ActiveMainContentView != Utilities.ViewCatalog.MainContentView.Inbox)
            {
                return false;
            }

            LoadAllLocalData();
            //Debug.WriteLine("InboxViewModel: returned true on HandleDataStateChanged!");
            return true;
        }
    }
}
