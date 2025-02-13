using Caliburn.Micro;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;

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

        // saves updated task data to server on property change
        protected override async void OnExistingTaskPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string? changedProperty = e.PropertyName;
            TaskDisplayModel senderTask = (TaskDisplayModel)sender;
            _logger.Info($"{senderTask.TaskName}'s property {changedProperty} was changed.");

            // if a reorder update, need to prevent a remote data fetch until after the entire collection
            // has been updated. CanUpdateOrderIndices will only be true on the final task in collection
            if (changedProperty!.Contains("Index"))
            {
                if (!CanUpdateOrderingIndices) { return; }
                else
                {
                    List<TaskDisplayModel> tasksToUpdate = LocalTasks!.ToList();
                    await _dataService.UpdateCollectionOrderingIndices(tasksToUpdate);               
                }
            }
            else { await _dataService.UpdateTaskData(senderTask); }
        }

        protected override bool HandleDataStateChanged(string propertyName, IDataState dataState)
        {
            if (!dataRefreshTriggers.Contains(propertyName) || ActiveMainContentView != Utilities.ViewCatalog.MainContentView.Inbox)
            {
                return false;
            }

            LoadAllLocalData();
            Debug.WriteLine("InboxViewModel: returned true on HandleDataStateChanged!");
            return true;
        }
    }
}
