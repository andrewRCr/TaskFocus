using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.MainContent
{
    public class TodayViewModel : TaskViewModelBase, INotifyPropertyChanged
    {
        public TodayViewModel(IEventAggregator events,
                              IWindowManager window,
                              IDataState dataState,
                              IDataService dataService,
                              IDataHelper dataHelper) : base(events, window, dataState, dataService, dataHelper)
        {
            OrderingIndex = "TodayIndex";
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

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            await base.OnInitializeAsync(cancellationToken);

            // remove TodayIndex from any completed (but not CleanedUp) tasks from view if completed > 1 day ago
            var oldCompletedTodayTasks = _dataState.Tasks!.Where(x => x.TodayIndex != null && x.Completed && (x.DateCompleted < DateTime.Now.Date)).ToList();
            foreach (TaskDisplayModel task in oldCompletedTodayTasks)
            {
                // force update: will detect and remove TodayIndex, as well as shift other task indices accordingly if needed
                await _dataService.UpdateTaskData(task, true);
            }
        }

        protected override void LoadLocalTaskData()
        {
            if (_dataState.IsDataLoaded())
            {
                List<TaskDisplayModel> dueTasks = _dataState.Tasks!.Where(x =>
                    (x.DueDate <= DateTime.Now.Date) && !x.CleanedUp && (x.DateCompleted == null || x.DateCompleted == DateTime.Now.Date)).ToList();
                List<TaskDisplayModel> starredTasks = _dataState.Tasks!.Where(x =>
                    (x.Starred == true) && !x.CleanedUp && (x.DateCompleted == null || x.DateCompleted == DateTime.Now.Date)).ToList();
                List<TaskDisplayModel> todayTasks = dueTasks.Concat(starredTasks).ToList();

                // ensure any newly due/overdue tasks have a TodayIndex
                int todayTasksWithTodayIndexCount = todayTasks.Where(x => x.TodayIndex != null).ToList().Count();
                foreach (TaskDisplayModel task in todayTasks)
                {
                    if (task.DueDate <= DateTime.Now.Date && task.TodayIndex == null)
                    {
                        task.TodayIndex = todayTasksWithTodayIndexCount;
                        todayTasksWithTodayIndexCount++;
                    }
                }

                todayTasks = todayTasks.OrderBy(x => x.TodayIndex).ToList();
                LocalTasks = new ObservableCollection<TaskDisplayModel>(todayTasks);
                foreach (TaskDisplayModel task in LocalTasks!)
                {
                    task.PropertyChanged += OnExistingTaskPropertyChanged!; // subscribe to property changed event
                }

                ShowEmptyTaskListTutorialText = LocalTasks.Count == 0;
            }
        }

        protected override bool HandleDataStateChanged(string propertyName, IDataState dataState)
        {
            if (!dataRefreshTriggers.Contains(propertyName) || ActiveMainContentView != Utilities.ViewCatalog.MainContentView.Today)
            {
                return false;
            }

            LoadAllLocalData();
            Debug.WriteLine("TodayViewModel: returned true on HandleDataStateChanged!");
            return true;
        }
    }
}
