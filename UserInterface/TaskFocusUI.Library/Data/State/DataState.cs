using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.State
{
    public class DataState : IDataState, IDataStateInternal
    {
        public DataState() {}

        public event DataStateChangedHandler DataStateChanged = default!;

        public void InvokeDataStateChanged(string propertyName)
        {
            DataStateChanged?.Invoke(propertyName, this);
        }

        private DateTimeOffset _lastSync = DateTimeOffset.MinValue;
        public DateTimeOffset LastSync
        {
            get { return _lastSync; }
            set
            {
                _lastSync = value;
                DataStateChanged?.Invoke(nameof(LastSync), this);
            }
        }

        private UserDisplayModel? _currentUser;
        public UserDisplayModel? CurrentUser
        {
            get { return _currentUser; }
            set
            {
                _currentUser = value;
                DataStateChanged?.Invoke(nameof(CurrentUser), this);
            }
        }

        private UserSettingsDisplayModel? _userSettings = default!;
        public UserSettingsDisplayModel? UserSettings
        {
            get { return _userSettings; }
            set
            {
                _userSettings = value;
                DataStateChanged?.Invoke(nameof(UserSettings), this);
            }
        }

        private List<TaskDisplayModel>? _tasks;
        List<TaskDisplayModel>? IDataStateInternal.Tasks
        {
            get { return _tasks; } 
            set 
            { 
                _tasks = value;
                //DataStateChanged.Invoke(nameof(IDataStateInternal.Tasks), this);
            }
        }

        private List<TaskDisplayModel>? _workingTasks;
        List<TaskDisplayModel>? IDataStateInternal.WorkingTasks
        {
            get { return _workingTasks; }
            set { _workingTasks = value; }
        }

        //private List<TaskDisplayModel>? _tasks;
        //public List<TaskDisplayModel>? Tasks
        //{
        //    get { return _tasks; }
        //    set
        //    {
        //        _tasks = value;
        //        //DataStateChanged.Invoke(nameof(Tasks), this);
        //    }
        //}

        //private List<TaskDisplayModel>? _workingTasks;
        //public List<TaskDisplayModel>? WorkingTasks
        //{
        //    get { return _workingTasks; }
        //    set
        //    {
        //        _workingTasks = value;
        //        DataStateChanged?.Invoke(nameof(WorkingTasks), this);
        //    }
        //}

        private List<ProjectDisplayModel>? _projects;
        List<ProjectDisplayModel>? IDataStateInternal.Projects 
        { 
            get { return _projects; }
            set
            {
                _projects = value;
               // DataStateChanged.Invoke(nameof(IDataStateInternal.Projects), this);
            }
        }

        private List<ProjectDisplayModel>? _workingProjects;
        List<ProjectDisplayModel>? IDataStateInternal.WorkingProjects
        {
            get => _workingProjects;
            set { _workingProjects = value; }
        }

        private List<ContextDisplayModel>? _contexts;
        List<ContextDisplayModel>? IDataStateInternal.Contexts
        {
            get => _contexts;
            set
            {
                _contexts = value;
                //DataStateChanged?.Invoke(nameof(IDataStateInternal.Contexts), this);
            }
        }

        private List<ContextDisplayModel>? _workingContexts;
        List<ContextDisplayModel>? IDataStateInternal.WorkingContexts
        {
            get => _workingContexts;
            set { _workingContexts = value; }
        }

        public bool IsDataLoaded()
        {
            //return CurrentUser != null && UserSettings != null &&
            //    Tasks != null && Projects != null && Contexts != null;

            //bool loaded = CurrentUser != null && UserSettings != null &&
            //    _tasks != null && _workingTasks != null && 
            //    _projects != null && _workingProjects != null &&
            //    _contexts != null && _workingContexts != null;

            //if (!loaded)
            //{
            //    Console.WriteLine($"_tasks: {_tasks != null}");
            //    Console.WriteLine($"_workingTasks: {_workingTasks != null}");
            //    Console.WriteLine($"_projects: {_projects != null}");
            //    Console.WriteLine($"_workingProjects: {_workingProjects != null}");
            //    Console.WriteLine($"_contexts: {_contexts != null}");
            //    Console.WriteLine($"_workingContexts: {_workingContexts != null}");
            //}

            return CurrentUser != null && UserSettings != null &&
                _tasks != null && _workingTasks != null &&
                _projects != null && _workingProjects != null &&
                _contexts != null && _workingContexts != null;
        }

        public List<UserModel> ChangedUserData { get; set; } = new();
        public List<UserSettingsModel> ChangedUserSettingsData { get; set; } = new();
        public List<TaskDisplayModel> ChangedTaskData { get; set; } = new();
        public List<ProjectDisplayModel> ChangedProjectData { get; set; } = new();
        public List<ContextDisplayModel> ChangedContextData { get; set; } = new();

        public int TempTaskId { get; set; } = 0;
        public int TempProjectId { get; set; } = 0;
        public int TempContextId { get; set; } = 0;
    }
}
