using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
using TaskFocusUI.Library;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.Base
{
    public abstract class TaskViewModelBase : ViewModelBase, IHandle<AppWindowHeightChangedEvent>
    {
        protected IWindowManager _window;
        protected IDataState _dataState;
        protected IDataService _dataService;
        protected IDataHelper _dataHelper;

        private int _appWindowHeight;
        public int AppWindowHeight
        {
            get { return _appWindowHeight; }
            set
            {
                _appWindowHeight = value;
                NotifyOfPropertyChange(() => AppWindowHeight);
            }
        }

        private int _maxTaskScrollHeight;
        public int MaxTaskScrollHeight
        {
            get { return _maxTaskScrollHeight; }
            set
            {
                _maxTaskScrollHeight = value;
                NotifyOfPropertyChange(() => MaxTaskScrollHeight);
            }
        }

        private int _listBoxHeight;
        public int ListBoxHeight
        {
            get { return _listBoxHeight; }
            set
            {
                _listBoxHeight = value;
                NotifyOfPropertyChange(() => ListBoxHeight);
            }
        }

        private int _taskCount;
        public int TaskCount
        {
            get { return _taskCount; }
            set
            {
                _taskCount = value;
                NotifyOfPropertyChange(() => TaskCount);
            }
        }

        public TaskViewModelBase(IEventAggregator events,
                                 IAppState appState,
                                 IWindowManager window,
                                 IDataState dataState,
                                 IDataService dataService,
                                 IDataHelper dataHelper) : base(events, appState)
        {
            _events = events;
            _window = window;
            _dataState = dataState;
            _dataService = dataService;
            _dataHelper = dataHelper;

            _dataState.DataStateChanged += DataStateChanged;
            AppWindowHeight = (int)appState.AppWindowHeight;
        }

        public string? OrderingIndex { get; set; }

        private bool _canUpdateOrderingIndices;
        public bool CanUpdateOrderingIndices
        {
            get { return _canUpdateOrderingIndices; }
            set { _canUpdateOrderingIndices = value; }
        }

        public RelayCommand DeleteTaskCommand => new RelayCommand(async execute => await TryDeleteSelectedTask());

        protected async Task TryDeleteSelectedTask()
        {
            if (SelectedTaskItem != null)
            {
                await _dataService.DeleteTask(SelectedTaskItem);
            }
        }

        public RelayCommand ClearSelectedTaskProjectCommand => new RelayCommand(execute => ClearSelectedTaskProject());

        protected void ClearSelectedTaskProject()
        {
            if (SelectedTaskItem != null)
            {
                // will trigger an API remote data update call
                SelectedTaskItem.ProjectName = null;
            }
        }

        protected List<string> dataRefreshTriggers = new List<string> {
        nameof(IDataState.Tasks), nameof(IDataState.Projects), nameof(IDataState.Contexts) };

        // to be defined in child components as needed
        protected virtual bool HandleDataStateChanged(string propertyName, IDataState dataState)
        {
            return false;
        }

        private async void DataStateChanged(string propertyName, IDataState dataState)
        {
            bool changesOccured = HandleDataStateChanged(propertyName, dataState);
        }

        private ObservableCollection<TaskDisplayModel>? _localTasks;
        public ObservableCollection<TaskDisplayModel>? LocalTasks
        {
            get { return _localTasks; }
            set 
            { 
                _localTasks = value; 
                NotifyOfPropertyChange(()=> LocalTasks);
            }
        }

        private ObservableCollection<ProjectDisplayModel>? _localProjects;
        public ObservableCollection<ProjectDisplayModel>? LocalProjects
        {
            get { return _localProjects; }
            set
            {
                _localProjects = value;
                NotifyOfPropertyChange(() => LocalProjects);
            }
        }

        private ObservableCollection<ContextDisplayModel>? _localContexts;
        public ObservableCollection<ContextDisplayModel>? LocalContexts
        {
            get { return _localContexts; }
            set
            {
                _localContexts = value;
                NotifyOfPropertyChange(() => LocalContexts);
            }
        }

        private TaskDisplayModel? _selectedTaskItem;
        public TaskDisplayModel? SelectedTaskItem
        {
            get { return _selectedTaskItem; }
            set
            {
                _selectedTaskItem = value;
                NotifyOfPropertyChange(() => SelectedTaskItem);
            }
        }

        private ProjectDisplayModel? _selectedProject;
        public ProjectDisplayModel? SelectedProject
        {
            get { return _selectedProject; }
            set
            {
                _selectedProject = value;
                NotifyOfPropertyChange(() => SelectedProject);

                // debug
                string selectedProjectText = SelectedProject != null ? SelectedProject.ProjectName : "NULL";
                Debug.WriteLine($"SelectedProject: {selectedProjectText}");
            }
        }

        private ContextDisplayModel? _selectedContext;
        public ContextDisplayModel? SelectedContext
        {
            get { return _selectedContext; }
            set
            {
                _selectedContext = value;
                NotifyOfPropertyChange(() => SelectedContext);
            }
        }

        protected override async void OnViewLoaded(object view)
        {
            try
            {
                LoadAllLocalData();
            }
            catch (Exception ex)
            {
                dynamic settings = new ExpandoObject();
                settings.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                settings.ResizeMode = ResizeMode.NoResize;
                settings.Title = "Exception!";

                var status = IoC.Get<StatusInfoViewModel>();
                status.UpdateMessage($"{ex.Source} threw an exception:", ex.Message);
                await _window.ShowDialogAsync(status, null, settings);
                await TryCloseAsync();
            }
        }

        public bool IsLocalDataLoaded()
        {
            return LocalTasks != null && LocalProjects != null && LocalContexts != null;
        }

        protected void LoadAllLocalData()
        {
            LoadLocalTaskData();
            LoadLocalProjectData();
            LoadLocalContextData();

            PerformTaskCleanup();
        }

        protected void PerformTaskCleanup()
        {
            if (LocalTasks != null && _dataState.IsDataLoaded())
            {
                foreach (TaskDisplayModel task in LocalTasks)
                {
                    if (task.Completed)
                    {
                        TimeSpan interval = (DateTime.Now - (DateTime)task.DateCompleted!);
                        int daysPassedSinceTaskCompletion = interval.Days;

                        // check if should delete
                        int deleteIntervalSetting = _dataState.UserSettings.DeleteDelayDays;
                        if (daysPassedSinceTaskCompletion > deleteIntervalSetting)
                        {
                            Task.Run(() => _dataService.DeleteTask(task).Wait());
                        }

                        // handle CleanedUp status
                        else if (!task.CleanedUp)
                        {
                            if (_dataState.UserSettings.CleanUpImmediately) { task.CleanedUp = true; }
                            else
                            {
                                int cleanupIntervalSetting = _dataState.UserSettings.CleanUpDelayDays;
                                if (daysPassedSinceTaskCompletion > cleanupIntervalSetting) { task.CleanedUp = true; }
                            }
                        }
                    }
                }
            }
        }

        protected virtual void LoadLocalTaskData()
        {
            if (_dataState.IsDataLoaded())
            {
                LocalTasks = new ObservableCollection<TaskDisplayModel>(_dataState.Tasks!);
                foreach (TaskDisplayModel task in LocalTasks!)
                {
                    task.PropertyChanged += OnExistingTaskPropertyChanged!; // subscribe to property changed event
                }
                TaskCount = LocalTasks.Count;
                UpdateScrollHeight(AppWindowHeight);
            }
        }

        protected virtual void LoadLocalProjectData()
        {
            if (_dataState.IsDataLoaded())
            {
                LocalProjects = new ObservableCollection<ProjectDisplayModel>(_dataState.Projects!.OrderBy(x => x.OrderIndex));
                foreach (ProjectDisplayModel project in LocalProjects!)
                {
                    project.PropertyChanged += OnExistingProjectPropertyChanged!; // subscribe to property changed event
                }
            }
        }

        protected virtual void LoadLocalContextData()
        {
            if (_dataState.IsDataLoaded())
            {
                LocalContexts = new ObservableCollection<ContextDisplayModel>(_dataState.Contexts!.OrderBy(x => x.OrderIndex));
                foreach (ContextDisplayModel context in LocalContexts!)
                {
                    context.PropertyChanged += OnExistingContextPropertyChanged!; // subscribe to property changed event
                }
            }
        }

        // saves updated task data to server on property change
        protected virtual async void OnExistingTaskPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string? changedProperty = e.PropertyName;
            TaskDisplayModel senderTask = (TaskDisplayModel)sender;
            _logger.Info($"{senderTask.TaskName}'s property {changedProperty} was changed.");

            await _dataService.UpdateTaskData(senderTask);
        }

        // saves updated project data to server on property change
        protected virtual async void OnExistingProjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string? changedProperty = e.PropertyName;
            ProjectDisplayModel senderProject = (ProjectDisplayModel)sender;
            _logger.Info($"{senderProject.ProjectName}'s property {changedProperty} was changed.");

            // only ProjectSubNavMenuVieWModel handles reorder updates
            if (changedProperty!.Contains("Index")) { return; }
            else { await _dataService.UpdateProjectData(senderProject); }
        }

        // saves updated context data to server on property change
        protected virtual async void OnExistingContextPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string? changedProperty = e.PropertyName;
            ContextDisplayModel senderContext = (ContextDisplayModel)sender;
            _logger.Info($"{senderContext.ContextName}'s property {changedProperty} was changed.");

            await _dataService.UpdateContextData(senderContext);
        }

        // updates task listbox and containing scrollviewer height values dynamically
        protected virtual void UpdateScrollHeight(int appWindowHeight)
        {
            int fixedTotalOtherWindowElementsHeight = 300;
            int requiredTaskListHeight = 70 * TaskCount;
            MaxTaskScrollHeight = requiredTaskListHeight;

            if (appWindowHeight - fixedTotalOtherWindowElementsHeight < requiredTaskListHeight)
            {
                int difference = requiredTaskListHeight - (appWindowHeight - fixedTotalOtherWindowElementsHeight);
                ListBoxHeight = requiredTaskListHeight - difference;
            }
            else
            {
                ListBoxHeight = requiredTaskListHeight;
            }

            AppWindowHeight = appWindowHeight;
        }

        public Task HandleAsync(AppWindowHeightChangedEvent message, CancellationToken cancellationToken)
        {
            UpdateScrollHeight((int)message.NewAppWindowHeight);
            return Task.CompletedTask;
        }
    }
}
