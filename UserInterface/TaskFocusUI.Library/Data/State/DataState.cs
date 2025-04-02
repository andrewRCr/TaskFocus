using System;
using System.Collections.Generic;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.State
{
    public enum EDataRefreshType
    {
        User,
        Settings,
        Tasks,
        Projects,
        Contexts,
    }

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
        UserDisplayModel? IDataStateInternal.CurrentUser
        {
            get { return _currentUser; }
            set
            {
                _currentUser = value;
                DataStateChanged?.Invoke(nameof(EDataRefreshType.User), this);
            }
        }

        private UserDisplayModel? _workingCurrentUser;
        UserDisplayModel? IDataStateInternal.WorkingCurrentUser
        {
            get { return _workingCurrentUser; }
            set { _workingCurrentUser = value; }
        }

        private UserSettingsDisplayModel? _userSettings = default!;
        UserSettingsDisplayModel? IDataStateInternal.UserSettings
        {
            get { return _userSettings; }
            set
            {
                _userSettings = value;
                DataStateChanged?.Invoke(nameof(EDataRefreshType.Settings), this);
            }
        }

        private UserSettingsDisplayModel? _workingUserSettings;
        UserSettingsDisplayModel? IDataStateInternal.WorkingUserSettings
        {
            get { return _workingUserSettings; }
            set { _workingUserSettings = value; }
        }

        private List<TaskDisplayModel>? _tasks;
        List<TaskDisplayModel>? IDataStateInternal.Tasks
        {
            get { return _tasks; } 
            set 
            {  
                _tasks = value;
                DataStateChanged?.Invoke(nameof(EDataRefreshType.Tasks), this);
            }
        }

        private List<TaskDisplayModel>? _workingTasks;
        List<TaskDisplayModel>? IDataStateInternal.WorkingTasks
        {
            get { return _workingTasks; }
            set { _workingTasks = value; }
        }

        private List<ProjectDisplayModel>? _projects;
        List<ProjectDisplayModel>? IDataStateInternal.Projects 
        { 
            get { return _projects; }
            set 
            {
                _projects = value;
                DataStateChanged?.Invoke(nameof(EDataRefreshType.Projects), this);
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
                DataStateChanged?.Invoke(nameof(EDataRefreshType.Contexts), this);
            }
        }

        private List<ContextDisplayModel>? _workingContexts;
        List<ContextDisplayModel>? IDataStateInternal.WorkingContexts
        {
            get => _workingContexts;
            set { _workingContexts = value; }
        }

        bool IDataStateInternal.IsDataLoaded()
        {
            return _currentUser != null && _workingCurrentUser != null &&
                   _userSettings != null && _workingUserSettings != null &&
                   _tasks != null && _workingTasks != null &&
                   _projects != null && _workingProjects != null &&
                   _contexts != null && _workingContexts != null;
        }

        public UserDisplayModel? ChangedUserData { get; set; } = new();
        public UserSettingsDisplayModel? ChangedUserSettingsData { get; set; } = new();
        public List<TaskDisplayModel> ChangedTaskData { get; set; } = new();
        public List<ProjectDisplayModel> ChangedProjectData { get; set; } = new();
        public List<ContextDisplayModel> ChangedContextData { get; set; } = new();

        public int TempTaskId { get; set; } = 0;
        public int TempProjectId { get; set; } = 0;
        public int TempContextId { get; set; } = 0;
    }
}
