using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.EventModels;
using TaskFocusDesktop.Utilities;
using TaskFocusUI.Library.Data.Services.Access;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Data.Utilities;
using TaskFocusUI.Library.Models;

namespace TaskFocusDesktop.ViewModels.Base
{
    public abstract class TaskViewModelBase : ViewModelBase, IHandle<AppWindowHeightChangedEvent>
    {
        protected IWindowManager _window;
        protected IDataState _dataState;
        protected IDataService _dataService;
        protected IDataHelper _dataHelper;

        protected List<string> dataRefreshTriggers = new List<string> {
            nameof(EDataRefreshType.Tasks), nameof(EDataRefreshType.Projects), nameof(EDataRefreshType.Contexts) };

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
        public RelayCommand DeleteTaskCommand => new RelayCommand(execute => TryDeleteSelectedTask());
        public RelayCommand ClearSelectedTaskProjectCommand => new RelayCommand(execute => ClearSelectedTaskProject());

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

        private bool _canUpdateOrderingIndices;
        public bool CanUpdateOrderingIndices
        {
            get { return _canUpdateOrderingIndices; }
            set { _canUpdateOrderingIndices = value; }
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
                //string selectedProjectText = SelectedProject != null ? SelectedProject.ProjectName : "NULL";
                //_logger.Info($"SelectedProject: {selectedProjectText}");
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

        protected void TryDeleteSelectedTask()
        {
            if (SelectedTaskItem != null)
            {
                _dataService.DeleteTask(SelectedTaskItem);
            }
        }

        protected void ClearSelectedTaskProject()
        {
            if (SelectedTaskItem != null)
            {
                // will trigger an API remote data update call
                SelectedTaskItem.ProjectName = null;
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
        }

        protected virtual void LoadLocalTaskData()
        {
            if (_dataService.IsDataStateLoaded())
            {
                LocalTasks = new ObservableCollection<TaskDisplayModel>(_dataService.GetDataStateTasks()!);
                SubscribeToTaskPropertyChangedEvents(LocalTasks);
                TaskCount = LocalTasks.Count;
                UpdateScrollHeight(AppWindowHeight);
            }
        }

        protected virtual void LoadLocalProjectData()
        {
            if (_dataService.IsDataStateLoaded())
            {
                var projects = _dataService.GetDataStateProjects()!;
                projects.Sort((a, b) => Nullable.Compare(a.OrderIndex, b.OrderIndex));
                LocalProjects = new ObservableCollection<ProjectDisplayModel>(projects);              
            }
        }

        protected virtual void LoadLocalContextData()
        {
            if (_dataService.IsDataStateLoaded())
            {
                var contexts = _dataService.GetDataStateContexts()!;
                contexts.Sort((a, b) => Nullable.Compare(a.OrderIndex, b.OrderIndex));
                LocalContexts = new ObservableCollection<ContextDisplayModel>(contexts);
            }
        }

        protected void SubscribeToTaskPropertyChangedEvents(ObservableCollection<TaskDisplayModel>? tasks)
        {
            if (tasks == null) return;
            foreach (TaskDisplayModel task in tasks)
            {
                // ensure only one subscriber
                task.PropertyChanged -= OnExistingTaskPropertyChanged!;
                task.PropertyChanged += OnExistingTaskPropertyChanged!;
            }
        }

        protected void UnsubscribeFromTaskPropertyChangedEvents(ObservableCollection<TaskDisplayModel>? tasks)
        {
            if (tasks == null) return;
            foreach (TaskDisplayModel task in tasks) task.PropertyChanged -= OnExistingTaskPropertyChanged!;
        }

        protected void SubscribeToProjectPropertyChangedEvents(ObservableCollection<ProjectDisplayModel>? projects)
        {
            if (projects == null) return;
            foreach (ProjectDisplayModel project in projects)
            {
                // ensure only one subscriber
                project.PropertyChanged -= OnExistingProjectPropertyChanged!;
                project.PropertyChanged += OnExistingProjectPropertyChanged!;
            }
        }

        protected void UnsubscribeFromProjectPropertyChangedEvents(ObservableCollection<ProjectDisplayModel>? projects)
        {
            if (projects == null) return;
            foreach (ProjectDisplayModel project in projects) project.PropertyChanged -= OnExistingProjectPropertyChanged!;
        }

        protected void SubscribeToContextPropertyChangedEvents(ObservableCollection<ContextDisplayModel>? contexts)
        {
            foreach (ContextDisplayModel context in contexts!)
            {
                // ensure only one subscriber
                context.PropertyChanged -= OnExistingContextPropertyChanged!;
                context.PropertyChanged += OnExistingContextPropertyChanged!;
            }
        }

        protected void UnsubscribeFromContextPropertyChangedEvents(ObservableCollection<ContextDisplayModel>? contexts)
        {
            if (contexts == null) return;
            foreach (ContextDisplayModel context in contexts) context.PropertyChanged -= OnExistingContextPropertyChanged!;
        }

        // saves updated task data to local data state on property change
        protected virtual async void OnExistingTaskPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            await VerifyAuthAndRedirectIfExpired();

            string? changedProperty = e.PropertyName;
            TaskDisplayModel senderTask = (TaskDisplayModel)sender;

            if (!_dataService.IsTaskCurrentlyBeingUpdated(senderTask))
            {
                if (changedProperty!.Contains("Index") && CanUpdateOrderingIndices)

                //_logger.Info($"{this.ToString()}: workingTask property changed: {senderTask.TaskName}'s property {changedProperty} was changed.");
                _dataService.UpdateTaskData(senderTask);
            }
        }

        // saves updated project data to local data state on property change
        protected virtual async void OnExistingProjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            await VerifyAuthAndRedirectIfExpired();

            string? changedProperty = e.PropertyName;
            ProjectDisplayModel senderProject = (ProjectDisplayModel)sender;

            if (!_dataService.IsProjectCurrentlyBeingUpdated(senderProject))
            {
                //_logger.Info($"workingProject property changed: {senderProject.ProjectName}'s property {changedProperty} was changed.");

                // only ProjectSubNavMenuViewModel handles reorder updates
                if (changedProperty!.Contains("Index")) return;
                else _dataService.UpdateProjectData(senderProject);
            }
        }

        // saves updated context data to local data state on property change
        protected virtual async void OnExistingContextPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            await VerifyAuthAndRedirectIfExpired();

            string? changedProperty = e.PropertyName;
            ContextDisplayModel senderContext = (ContextDisplayModel)sender;

            if (!_dataService.IsContextCurrentlyBeingUpdated(senderContext))
            {
                //_logger.Info($"workingContext property changed: {senderContext.ContextName}'s property {changedProperty} was changed.");

                // only ContextSubNavMenuViewModel handles reorder updates
                if (changedProperty!.Contains("Index"))return;
                else _dataService.UpdateContextData(senderContext);
            }
        }

        // to be defined in child components as needed; does nothing by default
        private async void DataStateChanged(string propertyName, IDataState dataState)
        {
            bool changesOccured = HandleDataStateChanged(propertyName, dataState);
            if (changesOccured)
            {
                //await InvokeAsync(StateHasChanged);
            }
        }

        // to be defined in child components as needed
        protected virtual bool HandleDataStateChanged(string propertyName, IDataState dataState)
        {
            return false;
        }

        // updates task listbox and containing scrollviewer height values dynamically
        protected virtual void UpdateScrollHeight(int appWindowHeight)
        {
            int fixedTotalOtherWindowElementsHeight = 250;
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

        // AppWindowHeightChangedEvent handler
        public Task HandleAsync(AppWindowHeightChangedEvent message, CancellationToken cancellationToken)
        {
            UpdateScrollHeight((int)message.NewAppWindowHeight);
            return Task.CompletedTask;
        }

        // ViewSwitchedEvent handler
        public override async Task HandleAsync(ViewSwitchedEvent message, CancellationToken cancellationToken)
        {
            if (message.SwitchedContentPanel == ViewCatalog.ContentPanel.MainContent)
            {
                _dataState.DataStateChanged += DataStateChanged;
                UnsubscribeFromTaskPropertyChangedEvents(LocalTasks);
            }

            await base.HandleAsync(message, cancellationToken);
        }
    }
}
