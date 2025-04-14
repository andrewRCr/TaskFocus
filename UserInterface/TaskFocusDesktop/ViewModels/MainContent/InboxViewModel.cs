using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Utilities;
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

        private ObservableCollection<TaskDisplayModel>? _inboxTasks;
        public ObservableCollection<TaskDisplayModel>? InboxTasks
        {
            get { return _inboxTasks; }
            set
            {
                _inboxTasks = value;
                NotifyOfPropertyChange(() => InboxTasks);
            }
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
            if (_dataService.IsDataStateLoaded())
            {
                List<TaskDisplayModel> unorderedInboxTasks = _dataService.GetDataStateTasks()!.Where(
                    x => (x.ProjectId == null || x.ContextId == null) && !x.CleanedUp).ToList();
                InboxTasks = new ObservableCollection<TaskDisplayModel>(unorderedInboxTasks.OrderBy(x => x.InboxIndex).ToList());
                SubscribeToTaskPropertyChangedEvents(InboxTasks);

                ShowEmptyTaskListTutorialText = InboxTasks.Count == 0;
                TaskCount = InboxTasks.Count;
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
            //_logger.Info($"InboxViewModel: returned true on HandleDataStateChanged! due to property: {propertyName}");
            return true;
        }

        // ViewSwitchedEvent handler
        public override async Task HandleAsync(ViewSwitchedEvent message, CancellationToken cancellationToken)
        {
            if (message.SwitchedContentPanel == ViewCatalog.ContentPanel.MainContent)
            {
                UnsubscribeFromTaskPropertyChangedEvents(InboxTasks);
            }

            await base.HandleAsync(message, cancellationToken);
        }
    }
}
