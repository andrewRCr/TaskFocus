using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public CompletedViewModel(IEventAggregator events,
                                  IWindowManager window, 
                                  IDataState dataState,
                                  IDataService dataService,
                                  IDataHelper dataHelper) : base(events, window, dataState, dataService, dataHelper)
        {
        }

        private BindingList<string>? _completedTaskNames;
        public BindingList<string>? CompletedTaskNames
        {
            get { return _completedTaskNames; }
            set
            {
                _completedTaskNames = value;
                NotifyOfPropertyChange(() => CompletedTaskNames);
            }
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            if (IsLocalDataLoaded())
            {
                CompletedTaskNames = new BindingList<string>();

                foreach (var item in LocalTasks!)
                {
                    CompletedTaskNames!.Add(item.TaskName);
                }

                NotifyOfPropertyChange(() => CompletedTaskNames);
            }
        }

        protected override void LoadLocalTaskData()
        {
            if (_dataState.IsDataLoaded())
            {
                List<TaskDisplayModel> completedTasks = _dataState.Tasks!.Where(x => x.Completed).ToList();

                completedTasks.OrderBy(x => x.DateCompleted);
                //LocalTasks = new BindingList<TaskDisplayModel>(completedTasks);
                LocalTasks = new ObservableCollection<TaskDisplayModel>(completedTasks);
                foreach (TaskDisplayModel task in LocalTasks!)
                {
                    task.PropertyChanged += OnExistingTaskPropertyChanged!; // subscribe to property changed event
                }
            }
        }
    }
}
