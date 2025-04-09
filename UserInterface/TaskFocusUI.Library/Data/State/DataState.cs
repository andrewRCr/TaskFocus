using System;
using System.Collections.Generic;
using TaskFocusUI.Library.Data.Services.Synchronization;
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
        AppRequestedSyncCompleted
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
        DateTimeOffset IDataStateInternal.LastSync
        {
            get { return _lastSync; }
            set
            {
                _lastSync = value;
                DataStateChanged?.Invoke(nameof(IDataStateInternal.LastSync), this);
            }
        }

        //private EPostSyncAction _requestedPostSyncAction = EPostSyncAction.None;
        //EPostSyncAction IDataStateInternal.RequestedPostSyncAction
        //{
        //    get { return _requestedPostSyncAction; }
        //    set
        //    {
        //        _requestedPostSyncAction = value;
        //        DataStateChanged?.Invoke(nameof(EDataRefreshType.RequestedPostSyncAction), this);
        //    }
        //}

        private bool _appRequestedSyncCompleted = false;
        bool IDataStateInternal.AppRequestedSyncCompleted
        {
            get { return _appRequestedSyncCompleted; }
            set
            {
                _appRequestedSyncCompleted = value;
                DataStateChanged?.Invoke(nameof(EDataRefreshType.AppRequestedSyncCompleted), this);
            }
        }

        //private bool _preLogoutSyncCompleted = false;
        //bool IDataStateInternal.PreLogoutSyncCompleted
        //{
        //    get { return _preLogoutSyncCompleted; }
        //    set
        //    {
        //        _preLogoutSyncCompleted = value;
        //        DataStateChanged?.Invoke(nameof(EDataRefreshType.PreLogoutSyncCompleted), this);
        //    }
        //}

        //private bool _preAppCloseSyncCompleted = false;
        //bool IDataStateInternal.PreAppCloseSyncCompleted
        //{
        //    get { return _preAppCloseSyncCompleted; }
        //    set
        //    {
        //        _preAppCloseSyncCompleted = value;
        //        DataStateChanged?.Invoke(nameof(EDataRefreshType.PreAppCloseSyncCompleted), this);
        //    }
        //}

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

        // tracking pre-push local updates
        public UserDisplayModel? ChangedUserData { get; set; }
        public UserSettingsDisplayModel? ChangedUserSettingsData { get; set; }
        public List<TaskDisplayModel> ChangedTaskData { get; set; } = new();
        public List<ProjectDisplayModel> ChangedProjectData { get; set; } = new();
        public List<ContextDisplayModel> ChangedContextData { get; set; } = new();

        // tracking pre-push local insertions
        public int TempTaskId { get; set; } = 0;
        public int TempProjectId { get; set; } = 0;
        public int TempContextId { get; set; } = 0;
    }
}
