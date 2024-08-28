using AutoMapper;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TaskFocusDesktop.Commands;
using TaskFocusDesktop.Utilities;
using TaskFocusUI.Library;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;

namespace TaskFocusDesktop.ViewModels.Base
{
    public abstract class TaskViewModelBase : ViewModelBase
    {
        protected IWindowManager _window;
        protected IDataState _dataState;
        protected IDataService _dataService;
        protected IDataHelper _dataHelper;

        public TaskViewModelBase(IEventAggregator events, IWindowManager window, IDataState dataState, IDataService dataService, IDataHelper dataHelper) : base(events)
        {
            _events = events;
            _window = window;
            _dataState = dataState;
            _dataService = dataService;
            _dataHelper = dataHelper;

            _dataState.DataStateChanged += DataStateChanged;
        }

        public string? OrderingIndex { get; set; }

        private bool _canUpdateOrderingIndices;
        public bool CanUpdateOrderingIndices
        {
            get { return _canUpdateOrderingIndices; }
            set { _canUpdateOrderingIndices = value; }
        }

        //public RelayCommand DeleteTaskCommand => new RelayCommand(async execute => await Task.CompletedTask);

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
            //if (changesOccured)
            //{
            //    //await InvokeAsync(StateHasChanged);
            //    Refresh();
            //}
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

        private BindingList<ContextDisplayModel>? _localContexts;
        public BindingList<ContextDisplayModel>? LocalContexts
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

                // debug
                //string selectedTaskItemText = SelectedTaskItem != null ? SelectedTaskItem.TaskName : "NULL";
                //Debug.WriteLine($"SelectedTaskItem: {selectedTaskItemText}");
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
                //Debug.WriteLine($"SelectedProject: {selectedProjectText}");
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

                // debug
                //string selectedContextText = SelectedContext != null ? SelectedContext.ContextName : "NULL";
                //Debug.WriteLine($"SelectedContext: { selectedContextText}");
            }
        }

        protected override async void OnViewLoaded(object view)
        {
            // nothing local, currently.
            // children will assign themselves as the ActiveViewModel
            // and then call LoadTasks()

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
                //LocalTasks = new BindingList<TaskDisplayModel>(_dataState.Tasks!);
                LocalTasks = new ObservableCollection<TaskDisplayModel>(_dataState.Tasks!);
                foreach (TaskDisplayModel task in LocalTasks!)
                {
                    task.PropertyChanged += OnExistingTaskPropertyChanged!; // subscribe to property changed event
                }
            }
        }

        protected virtual void LoadLocalProjectData()
        {
            if (_dataState.IsDataLoaded())
            {
                //LocalProjects = new BindingList<ProjectDisplayModel>(_dataState.Projects!);
                LocalProjects = new ObservableCollection<ProjectDisplayModel>(_dataState.Projects!);
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
                LocalContexts = new BindingList<ContextDisplayModel>(_dataState.Contexts!);
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
        protected async void OnExistingProjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string? changedProperty = e.PropertyName;
            ProjectDisplayModel senderProject = (ProjectDisplayModel)sender;
            _logger.Info($"{senderProject.ProjectName}'s property {changedProperty} was changed.");

            await _dataService.UpdateProjectData(senderProject);
        }

        // saves updated context data to server on property change
        protected async void OnExistingContextPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            string? changedProperty = e.PropertyName;
            ContextDisplayModel senderContext = (ContextDisplayModel)sender;
            _logger.Info($"{senderContext.ContextName}'s property {changedProperty} was changed.");

            await _dataService.UpdateContextData(senderContext);
        }

        //private BindingList<TaskDisplayModel> _newTaskList;
        //public BindingList<TaskDisplayModel> NewTaskList
        //{
        //    get { return _newTaskList; }
        //    set
        //    {
        //        _newTaskList = value;
        //        NotifyOfPropertyChange(() => NewTaskList);
        //    }
        //}

        //private TaskDisplayModel _newTask;
        //public TaskDisplayModel NewTask
        //{
        //    get { return _newTask; }
        //    set
        //    {
        //        _newTask = value;
        //        NotifyOfPropertyChange(() => NewTask);
        //    }
        //}

        //public bool CanAddTask
        //{
        //    get
        //    {
        //        return NewTask != null ? !string.IsNullOrWhiteSpace(NewTask.TaskName) : false;
        //    }
        //}

        //protected async Task LoadTasks()
        //{
        //    Task<List<TaskModel>> loadTaskToCall = null;

        //    //switch (ActiveViewModel)
        //    //{
        //    //    case ViewModelChildren.InboxVM:
        //    //        loadTaskToCall = _taskEndpoint.GetInboxTasksForUser();
        //    //        break;
        //    //    default:
        //    //        loadTaskToCall = _taskEndpoint.GetAllTasksForUser();
        //    //        break;
        //    //}

        //    //var taskList = await loadTaskToCall;
        //    //TasksLastFetch = taskList; // store for comparison

        //    //var displayTaskList = _mapper.Map<List<TaskDisplayModel>>(taskList);
        //    //Tasks = new BindingList<TaskDisplayModel>(displayTaskList);

        //    //foreach (TaskDisplayModel displayTask in Tasks)
        //    //{
        //    //    displayTask.PropertyChanged += OnExistingTaskPropertyChanged; // subscribe to property changed event
        //    //}

        //    //var projectList = await _projectEndpoint.GetAllProjectsForUser();
        //    //Projects = new BindingList<ProjectModel>(projectList);

        //    //var contextList = await _contextEndpoint.GetAllContextsForUser();
        //    //Contexts = new BindingList<ContextModel>(contextList);

        //    //// new task input placeholder
        //    //List<TaskDisplayModel> newTaskList = new List<TaskDisplayModel>();
        //    //TaskDisplayModel newTaskPlaceholder = new TaskDisplayModel();
        //    //NewTask = newTaskPlaceholder;
        //    //NewTask.PropertyChanged += OnNewTaskPropertyChanged; // subscribe to property changed event
        //    //newTaskList.Add(newTaskPlaceholder);
        //    //NewTaskList = new BindingList<TaskDisplayModel>(newTaskList);
        //}

        //public async Task AssignProjectIdFromProjectName(TaskModel task)
        //{
        //    if (task.ProjectName != null)
        //    {
        //        // lookup projectId by projectName and assign
        //        // TODO: need to enforce uniqueness of the projectName property - casing, etc; something. ensure these will match!

        //        ProjectModel FindAssignedProject()
        //        {
        //            List<ProjectModel> userProjects = Projects.ToList();
        //            return userProjects.Find(x => x.ProjectName == task.ProjectName);
        //        }

        //        ProjectModel assignedProject = FindAssignedProject();
        //        if (assignedProject == null)
        //        {
        //            ProjectModel newProject = new ProjectModel { ProjectName = task.ProjectName };
        //            await AddProject(newProject);

        //            assignedProject = FindAssignedProject();
        //        }

        //        task.ProjectId = assignedProject.Id;
        //    }
        //}

        //public async Task AssignContextIdFromContextName(TaskModel task)
        //{
        //    if (task.ContextName != null)
        //    {
        //        // lookup ContextId by contextName and assign
        //        // TODO: need to enforce uniqueness of the contextName property - casing, etc; something. ensure these will match!

        //        ContextModel FindAssignedContext()
        //        {
        //            List<ContextModel> userContexts = Contexts.ToList();
        //            return userContexts.Find(x => x.ContextName == task.ContextName);
        //        }

        //        ContextModel assignedContext = FindAssignedContext();

        //        if (assignedContext == null)
        //        {
        //            ContextModel newContext = new ContextModel { ContextName = task.ContextName };
        //            await AddContext(newContext);

        //            assignedContext = FindAssignedContext();
        //        }

        //        task.ContextId = assignedContext.Id;
        //    }
        //}

        //public async Task AddTask()
        //{
        //    if (string.IsNullOrWhiteSpace(NewTask.TaskName)) { return; }

        //    // map from TaskDisplayModel to TaskModel
        //    TaskModel newTask = _mapper.Map<TaskModel>(NewTask);

        //    if (newTask.ProjectName != null)
        //    {
        //        await AssignProjectIdFromProjectName(newTask);
        //        // TODO: need to enforce uniqueness of the projectName property - casing, etc; something. ensure these will match!
        //    }

        //    if (newTask.ContextName != null)
        //    {
        //        await AssignContextIdFromContextName(newTask);
        //        // TODO: need to enforce uniqueness of the contextName property - casing, etc; something. ensure these will match!
        //    }

        //    await _taskEndpoint.AddTask(newTask, _apiHelper.GetLoggedInUserId());

        //    // refresh Tasks + clear NewTask
        //    await LoadTasks();
        //}

        //public async Task DeleteTask()
        //{
        //    if (SelectedTask == null) { return; }

        //    // map from TaskDisplayModel to TaskModel
        //    TaskModel taskToDelete = _mapper.Map<TaskModel>(SelectedTask);

        //    await _taskEndpoint.DeleteTask(taskToDelete);

        //    // refresh Tasks + repopulate TasksLastFetch
        //    await LoadTasks();
        //}

        //// post updated task data to API for a single displayTask
        //public async Task UpdateTaskData(TaskDisplayModel displayTask)
        //{
        //    // map from TaskDisplayModel to TaskModel
        //    TaskModel task = _mapper.Map<TaskModel>(displayTask);

        //    if (_dataHelper.HasTaskDataChanged(task))
        //    {
        //        if (_dataHelper.HasTaskProjectNameChanged(task))
        //        {
        //            await AssignProjectIdFromProjectName(task);
        //        }

        //        if (_dataHelper.HasTaskContextNameChanged(task))
        //        {
        //            await AssignContextIdFromContextName(task);
        //        }

        //        await _taskEndpoint.UpdateTask(task);

        //        // refresh Tasks + repopulate TasksLastFetch
        //        await LoadTasks();
        //    }
        //}

        //// saves updated task data to server on property change
        //private async void OnExistingTaskPropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    string changedProperty = e.PropertyName;
        //    TaskDisplayModel senderTask = (TaskDisplayModel)sender;
        //    // map from TaskDisplayModel to TaskModel
        //    //TaskModel task = _mapper.Map<TaskModel>(senderTask);

        //    await UpdateTaskData(senderTask);
        //}

        // saves updated task data to server on property change
        //protected async void OnExistingTaskPropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    string? changedProperty = e.PropertyName;
        //    TaskDisplayModel senderTask = (TaskDisplayModel)sender;
        //    _logger.Info($"{senderTask.TaskName}'s property {changedProperty} was changed.");

        //    await _dataService.UpdateTaskData(senderTask);
        //}

        //private void OnNewTaskPropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    string changedProperty = e.PropertyName;

        //    if (changedProperty == "TaskName")
        //    {
        //        NotifyOfPropertyChange(() => CanAddTask);
        //    }
        //}

        //public async Task AddProject(ProjectModel newProject)
        //{
        //    if (string.IsNullOrWhiteSpace(newProject.ProjectName)) { return; }

        //    await _projectEndpoint.AddProject(newProject, _apiHelper.GetLoggedInUserId());

        //    // refresh Tasks, Projects, Contexts + clear NewTask
        //    await LoadTasks();
        //}

        //public async Task AddContext(ContextModel newContext)
        //{
        //    if (string.IsNullOrWhiteSpace(newContext.ContextName)) { return; }

        //    await _contextEndpoint.AddContext(newContext, _apiHelper.GetLoggedInUserId());

        //    // refresh Tasks, Projects, Contexts + clear NewTask
        //    await LoadTasks();
        //}
    }
}
