using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Windows;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.MainContent
{
    public class CompletedViewModel : TaskViewModelBase, INotifyPropertyChanged
    {
        public CompletedViewModel(IEventAggregator events, IAppState appState, IWindowManager window,
                                  IDataState dataState, IDataService dataService, IDataHelper dataHelper) : base(events, appState, window, dataState, dataService, dataHelper)
        {   
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
            if (_dataState.IsDataLoaded())
            {
                List<TaskDisplayModel> completedTasks = _dataState.Tasks!.Where(x => x.Completed).ToList();

                completedTasks.OrderBy(x => x.DateCompleted);
                LocalTasks = new ObservableCollection<TaskDisplayModel>(completedTasks);
                foreach (TaskDisplayModel task in LocalTasks!)
                {
                    task.PropertyChanged += OnExistingTaskPropertyChanged!; // subscribe to property changed event
                }

                ShowEmptyTaskListTutorialText = LocalTasks.Count == 0;
            }
        }

        private void LoadCurrentSettingsStrings()
        {
            if (_dataState.IsDataLoaded())
            {
                string dayStr = _dataState.UserSettings.DeleteDelayDays > 1 ? " days" : " day";
                CurrentDeletionIntervalSettingStr = _dataState.UserSettings.DeleteDelayDays.ToString() + dayStr;

                if (_dataState.UserSettings.CleanUpImmediately) { CurrentCleanUpIntervalSettingStr = "immediate"; }
                else
                {
                    dayStr = _dataState.UserSettings.CleanUpDelayDays > 1 ? " days" : " day";
                    CurrentCleanUpIntervalSettingStr = _dataState.UserSettings.CleanUpDelayDays.ToString() + dayStr;
                }
            }
        }

        protected override bool HandleDataStateChanged(string propertyName, IDataState dataState)
        {
            if (!dataRefreshTriggers.Contains(propertyName) || ActiveMainContentView != Utilities.ViewCatalog.MainContentView.Completed)
            {
                return false;
            }

            LoadAllLocalData();
            Debug.WriteLine("CompletedViewModel: returned true on HandleDataStateChanged!");
            return true;
        }
    }
}
