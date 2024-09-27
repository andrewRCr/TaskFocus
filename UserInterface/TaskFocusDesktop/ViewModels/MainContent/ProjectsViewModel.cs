using AutoMapper;
using Caliburn.Micro;
using System;
using System.ComponentModel;
using System.Dynamic;
using System.Windows;
using TaskFocusDesktop.ViewModels.Base;
using TaskFocusDesktop.Utilities;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Utilities;
using TaskFocusUI.Library;
using System.Collections.Generic;
using TaskFocusUI.Library.Models;
using System.Linq;
using TaskFocusDesktop.EventModels;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Xaml.Behaviors;
using System.Windows.Threading;

namespace TaskFocusDesktop.ViewModels.MainContent
{
    public class ProjectsViewModel : TaskViewModelBase, INotifyPropertyChanged, IHandle<FocusedProjectChangedEvent>
    {
        private bool _showNoFocusedProjectTutorialText = false;
        public bool ShowNoFocusedProjectTutorialText
        {
            get { return _showNoFocusedProjectTutorialText; }
            set
            {
                _showNoFocusedProjectTutorialText = value;
                NotifyOfPropertyChange(() => ShowNoFocusedProjectTutorialText);
            }
        }

        private int? _focusedProjectId;
        public int? FocusedProjectId
        {
            get { return _focusedProjectId; }
            set 
            { 
                _focusedProjectId = value; 
                NotifyOfPropertyChange(() => FocusedProjectId);
            }
        }

        private string? _focusedProjectName;
        public string? FocusedProjectName
        {
            get { return _focusedProjectName; }
            set 
            { 
                _focusedProjectName = value;
                NotifyOfPropertyChange(() => FocusedProjectName);
            }
        }

        private ObservableCollection<TaskDisplayModel>?   _focusedProjectTasks;
        public ObservableCollection<TaskDisplayModel>? FocusedProjectTasks
        {
            get { return _focusedProjectTasks; }
            set 
            { 
                _focusedProjectTasks = value; 
                NotifyOfPropertyChange(() => FocusedProjectTasks);
            }
        }

        public ProjectsViewModel(IEventAggregator events, IAppState appState, IWindowManager window,
                                 IDataState dataState, IDataService dataService, IDataHelper dataHelper) : base(events, appState, window, dataState, dataService, dataHelper)
        {
            OrderingIndex = "ProjectIndex";
            _events.SubscribeOnPublishedThread(this);
        }

        public async Task HandleAsync(FocusedProjectChangedEvent message, CancellationToken cancellationToken)
        {
            FocusedProjectId = message.NewFocusedProjectId;
            await SetFocusedProjectProperties();
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            ShowNoFocusedProjectTutorialText = FocusedProjectId == null;
        }

        protected override async void LoadLocalTaskData()
        {
            // do not invoke base method; only load relevant project tasks
            await SetFocusedProjectProperties();
        }

        private async Task SetFocusedProjectProperties()
        {
            if (FocusedProjectId != null)
            { await _dataService.FetchRemoteProjectAndTasksById((int)FocusedProjectId); }

            if (_dataHelper.FocusedProject != null)
            {
                ShowNoFocusedProjectTutorialText = false;
                FocusedProjectName = _dataHelper.FocusedProject.ProjectName.ToUpper();
                var projectTasks = _dataHelper.FocusedProjectTasks;
                FocusedProjectTasks = new ObservableCollection<TaskDisplayModel>(projectTasks);

                foreach (TaskDisplayModel task in FocusedProjectTasks!)
                {
                    task.PropertyChanged += OnExistingTaskPropertyChanged!; // subscribe to property changed event
                }
            }
            else
            {
                FocusedProjectId = null; // may have been deleted
                FocusedProjectName = null;
                FocusedProjectTasks = null;
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
                    List<TaskDisplayModel> tasksToUpdate = FocusedProjectTasks!.ToList();
                    await _dataService.UpdateCollectionOrderingIndices(tasksToUpdate);
                }
            }
            else { await _dataService.UpdateTaskData(senderTask); }
        }

        protected override bool HandleDataStateChanged(string propertyName, IDataState dataState)
        {
            if (!dataRefreshTriggers.Contains(propertyName) || ActiveMainContentView != Utilities.ViewCatalog.MainContentView.Projects)
            {
                return false;
            }

            LoadLocalTaskData();
            Debug.WriteLine("ProjectsViewModel: returned true on HandleDataStateChanged!");
            return true;
        }
    }
}
