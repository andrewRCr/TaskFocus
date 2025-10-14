using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
    public class TodayViewModel : TaskViewModelBase, INotifyPropertyChanged
    {
        public TodayViewModel(IEventAggregator events,
                              IAppState appState,
                              IWindowManager window,
                              IDataState dataState,
                              IDataService dataService,
                              IDataHelper dataHelper) : base(events, appState, window, dataState, dataService, dataHelper)
        {
            OrderingIndex = "TodayIndex";
        }

        private ObservableCollection<TaskDisplayModel>? _todayTasks;
        public ObservableCollection<TaskDisplayModel>? TodayTasks
        {
            get { return _todayTasks; }
            set
            {
                _todayTasks = value;
                NotifyOfPropertyChange(() => TodayTasks);
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

        protected override void LoadLocalTaskData()
        {
            if (_dataService.IsDataStateLoaded())
            {
                var combinedTodayTasks = _dataService.GetTodayTasks();
                TodayTasks = new ObservableCollection<TaskDisplayModel>(combinedTodayTasks.OrderBy(x => x.TodayIndex).ToList());
                SubscribeToTaskPropertyChangedEvents(TodayTasks);
                LocalTasks = TodayTasks;

                ShowEmptyTaskListTutorialText = TodayTasks.Count == 0;
                TaskCount = TodayTasks.Count;
                UpdateScrollHeight(AppWindowHeight);
            }
        }

        protected override bool HandleDataStateChanged(string propertyName, IDataState dataState)
        {
            if (!dataRefreshTriggers.Contains(propertyName) || ActiveMainContentView != Utilities.ViewCatalog.MainContentView.Today)
            {
                return false;
            }

            LoadAllLocalData();
            //_logger.Info("TodayViewModel: returned true on HandleDataStateChanged!");
            return true;
        }

        // ViewSwitchedEvent handler
        public override async Task HandleAsync(ViewSwitchedEvent message, CancellationToken cancellationToken)
        {
            if (message.SwitchedContentPanel == ViewCatalog.ContentPanel.MainContent && 
                ActiveMainContentView == ViewCatalog.MainContentView.Today)
            {
                UnsubscribeFromTaskPropertyChangedEvents(TodayTasks);
            }

            await base.HandleAsync(message, cancellationToken);
        }
    }
}
