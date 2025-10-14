using Caliburn.Micro;
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
    public class CompletedViewModel : TaskViewModelBase, INotifyPropertyChanged
    {
        public CompletedViewModel(IEventAggregator events,
                                  IAppState appState,
                                  IWindowManager window,
                                  IDataState dataState,
                                  IDataService dataService,
                                  IDataHelper dataHelper) : base(events, appState, window, dataState, dataService, dataHelper)
        {   
        }

        private ObservableCollection<TaskDisplayModel>? _completedTasks;
        public ObservableCollection<TaskDisplayModel>? CompletedTasks
        {
            get { return _completedTasks; }
            set
            {
                _completedTasks = value;
                NotifyOfPropertyChange(() => CompletedTasks);
            }
        }

        private bool _showEmptyTaskListTutorialText = false;
        public bool ShowEmptyTaskListTutorialText
        {
            get { return _showEmptyTaskListTutorialText; }
            set
            {
                _showEmptyTaskListTutorialText= value;
                NotifyOfPropertyChange(() => ShowEmptyTaskListTutorialText);
            }
        }

        private string _currentDeletionIntervalSettingStr = string.Empty;
        public string CurrentDeletionIntervalSettingStr
        {
            get { return _currentDeletionIntervalSettingStr; }
            set
            {
                _currentDeletionIntervalSettingStr = value;
                NotifyOfPropertyChange(() => CurrentDeletionIntervalSettingStr);
            }
        }

        private string _currentCleanUpIntervalSettingStr = string.Empty;
        public string CurrentCleanUpIntervalSettingStr
        {
            get { return _currentCleanUpIntervalSettingStr; }
            set
            {
                _currentCleanUpIntervalSettingStr = value;
                NotifyOfPropertyChange(() => CurrentCleanUpIntervalSettingStr);
            }
        }

        private void LoadCurrentSettingsStrings()
        {
            if (_dataService.IsDataStateLoaded())
            {
                string dayStr = _dataService.GetDataStateUserSettings()!.DeleteDelayDays > 1 ? " days" : " day";
                CurrentDeletionIntervalSettingStr = _dataService.GetDataStateUserSettings()!.DeleteDelayDays.ToString() + dayStr;

                if (_dataService.GetDataStateUserSettings()!.CleanUpImmediately) { CurrentCleanUpIntervalSettingStr = "immediate"; }
                else
                {
                    dayStr = _dataService.GetDataStateUserSettings()!.CleanUpDelayDays > 1 ? " days" : " day";
                    CurrentCleanUpIntervalSettingStr = _dataService.GetDataStateUserSettings()!.CleanUpDelayDays.ToString() + dayStr;
                }
            }
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            if (IsLocalDataLoaded())
            {
                LoadCurrentSettingsStrings();
            }
        }

        protected override void LoadLocalTaskData()
        {
            if (_dataService.IsDataStateLoaded())
            {
                List<TaskDisplayModel> unorderedCompletedTasks = _dataService.GetDataStateTasks()!.Where(x => x.Completed).ToList();
           
                CompletedTasks = new ObservableCollection<TaskDisplayModel>(unorderedCompletedTasks.OrderBy(x => x.DateCompleted).ToList());
                SubscribeToTaskPropertyChangedEvents(CompletedTasks);
                LocalTasks = CompletedTasks;

                ShowEmptyTaskListTutorialText = CompletedTasks.Count == 0;
                TaskCount = CompletedTasks.Count;
                UpdateScrollHeight(AppWindowHeight);
            }
        }

        protected override bool HandleDataStateChanged(string propertyName, IDataState dataState)
        {
            if (!dataRefreshTriggers.Contains(propertyName) || ActiveMainContentView != Utilities.ViewCatalog.MainContentView.Completed)
            {
                return false;
            }

            LoadAllLocalData();
            //_logger.Info("CompletedViewModel: returned true on HandleDataStateChanged!");
            return true;
        }

        // ViewSwitchedEvent handler
        public override async Task HandleAsync(ViewSwitchedEvent message, CancellationToken cancellationToken)
        {
            if (message.SwitchedContentPanel == ViewCatalog.ContentPanel.MainContent &&
                ActiveMainContentView == ViewCatalog.MainContentView.Completed)
            {
                UnsubscribeFromTaskPropertyChangedEvents(CompletedTasks);
            }

            await base.HandleAsync(message, cancellationToken);
        }
    }
}
