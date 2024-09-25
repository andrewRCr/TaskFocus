using AutoMapper;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusUI.Library;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.MainContent
{
    public class ContextsViewModel : TaskViewModelBase, INotifyPropertyChanged, IHandle<FocusedContextChangedEvent>
    {
        private bool _showNoFocusedContextTutorialText = false;
        public bool ShowNoFocusedContextTutorialText
        {
            get { return _showNoFocusedContextTutorialText; }
            set
            {
                _showNoFocusedContextTutorialText = value;
                NotifyOfPropertyChange(() => ShowNoFocusedContextTutorialText);
            }
        }

        private int? _focusedContextId;
        public int? FocusedContextId
        {
            get { return _focusedContextId; }
            set
            {
                _focusedContextId = value;
                NotifyOfPropertyChange(() => FocusedContextId);
            }
        }

        private string? _focusedContextName;
        public string? FocusedContextName
        {
            get { return _focusedContextName; }
            set
            {
                _focusedContextName = value;
                NotifyOfPropertyChange(() => FocusedContextName);
            }
        }

        private ObservableCollection<TaskDisplayModel>? _focusedContextasks;
        public ObservableCollection<TaskDisplayModel>? FocusedContextTasks
        {
            get { return _focusedContextasks; }
            set
            {
                _focusedContextasks = value;
                NotifyOfPropertyChange(() => FocusedContextTasks);
            }
        }

        public ContextsViewModel(IEventAggregator events,
                                 IWindowManager window,
                                 IDataState dataState,
                                 IDataService dataService,
                                 IDataHelper dataHelper) : base(events, window, dataState, dataService, dataHelper)
        {
            OrderingIndex = "ContextIndex";
            _events.SubscribeOnPublishedThread(this);
        }

        public async Task HandleAsync(FocusedContextChangedEvent message, CancellationToken cancellationToken)
        {
            FocusedContextId = message.NewFocusedContextId;
            await SetFocusedContextProperties();
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            ShowNoFocusedContextTutorialText = FocusedContextId == null;
        }

        protected override async void LoadLocalTaskData()
        {
            // do not invoke base method; only load relevant context tasks
            await SetFocusedContextProperties();
        }

        private async Task SetFocusedContextProperties()
        {
            if (FocusedContextId != null)
            { await _dataService.FetchRemoteContextAndTasksById((int)FocusedContextId); }

            if (_dataHelper.FocusedContext != null)
            {
                ShowNoFocusedContextTutorialText = false;
                FocusedContextName = _dataHelper.FocusedContext.ContextName.ToUpper();
                var contextTasks = _dataHelper.FocusedContextTasks;
                FocusedContextTasks = new ObservableCollection<TaskDisplayModel>(contextTasks);

                foreach (TaskDisplayModel task in FocusedContextTasks!)
                {
                    task.PropertyChanged += OnExistingTaskPropertyChanged!; // subscribe to property changed event
                }
            }
            else
            {
                FocusedContextId = null; // may have been deleted
                FocusedContextName = null;
                FocusedContextTasks = null;
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
                    List<TaskDisplayModel> tasksToUpdate = FocusedContextTasks!.ToList();
                    await _dataService.UpdateCollectionOrderingIndices(tasksToUpdate);
                }
            }
            else { await _dataService.UpdateTaskData(senderTask); }
        }

        protected override bool HandleDataStateChanged(string propertyName, IDataState dataState)
        {
            if (!dataRefreshTriggers.Contains(propertyName) || ActiveMainContentView != Utilities.ViewCatalog.MainContentView.Contexts)
            {
                return false;
            }

            LoadLocalTaskData();
            Debug.WriteLine("ContextsViewModel: returned true on HandleDataStateChanged!");
            return true;
        }
    }
}