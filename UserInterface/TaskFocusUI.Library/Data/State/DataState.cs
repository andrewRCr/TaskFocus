using System;
using System.Collections.Generic;
using System.Configuration;
using TaskFocusUI.Library.Data.Utilities;
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
        SyncStatus,
        SyncResult,
        AppRequestedSyncCompleted
    }

    public enum ESyncStatus
    {
        Initializing,
        Synchronized,
        SyncInProgress
    }

    public class DataState : IDataState, IDataStateInternal
    {
        public DataState() {}

        public event DataStateChangedHandler DataStateChanged = default!;

        public void InvokeDataStateChanged(string propertyName)
        {
            DataStateChanged?.Invoke(propertyName, this);
        }

        private ESyncStatus _currentSyncStatus = ESyncStatus.Initializing;
        ESyncStatus IDataStateInternal.CurrentSyncStatus
        {
            get => _currentSyncStatus;
            set
            {
                _currentSyncStatus = value;
                DataStateChanged?.Invoke(nameof(EDataRefreshType.SyncStatus), this);
            }
        }

        private int _syncInterval = 180;
        int IDataStateInternal.SyncInterval
        {
            get => _syncInterval;
            set
            {
                _syncInterval = value;
                DataStateChanged?.Invoke(nameof(IDataStateInternal.SyncInterval), this);
            }
        }

        private DateTimeOffset _lastSync = DateTimeOffset.MinValue;
        DateTimeOffset IDataStateInternal.LastSync
        {
            get => _lastSync;
            set
            {
                _lastSync = value;
                DataStateChanged?.Invoke(nameof(IDataStateInternal.LastSync), this);
            }
        }

        private DataSyncResult _lastSyncResult = default;
        DataSyncResult IDataStateInternal.LastSyncResult
        {
            get => _lastSyncResult;
            set
            {
                _lastSyncResult = value;
                DataStateChanged?.Invoke(nameof(EDataRefreshType.SyncResult), this);
            }
        }

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

        private UserDisplayModel? _currentUser;
        UserDisplayModel? IDataStateInternal.CurrentUser
        {
            get =>  _currentUser;
            set => _currentUser = value; 
        }

        private UserDisplayModel? _workingCurrentUser;
        UserDisplayModel? IDataStateInternal.WorkingCurrentUser
        {
            get => _workingCurrentUser;
            set 
            {
                _workingCurrentUser = value;
                DataStateChanged?.Invoke(nameof(EDataRefreshType.User), this);
            }
        }

        private UserSettingsDisplayModel? _userSettings = default!;
        UserSettingsDisplayModel? IDataStateInternal.UserSettings
        {
            get => _userSettings; 
            set => _userSettings = value;
        }

        private UserSettingsDisplayModel? _workingUserSettings;
        UserSettingsDisplayModel? IDataStateInternal.WorkingUserSettings
        {
            get => _workingUserSettings;
            set 
            {
                _workingUserSettings = value;
                DataStateChanged?.Invoke(nameof(EDataRefreshType.Settings), this);
            }
        }

        private List<TaskDisplayModel>? _tasks;
        List<TaskDisplayModel>? IDataStateInternal.Tasks
        {
            get => _tasks;
            set => _tasks = value;
        }

        private List<TaskDisplayModel>? _workingTasks;
        List<TaskDisplayModel>? IDataStateInternal.WorkingTasks
        {
            get => _workingTasks;
            set 
            {
                _workingTasks = value;
                DataStateChanged?.Invoke(nameof(EDataRefreshType.Tasks), this);
            }
        }

        private List<ProjectDisplayModel>? _projects;
        List<ProjectDisplayModel>? IDataStateInternal.Projects
        {
            get => _projects;
            set => _projects = value;
        }

        private List<ProjectDisplayModel>? _workingProjects;
        List<ProjectDisplayModel>? IDataStateInternal.WorkingProjects
        {
            get => _workingProjects;
            set 
            {
                _workingProjects = value;
                DataStateChanged?.Invoke(nameof(EDataRefreshType.Projects), this);
            }
        }

        private List<ContextDisplayModel>? _contexts;
        List<ContextDisplayModel>? IDataStateInternal.Contexts
        {
            get => _contexts;
            set => _contexts = value;
        }

        private List<ContextDisplayModel>? _workingContexts;
        List<ContextDisplayModel>? IDataStateInternal.WorkingContexts
        {
            get => _workingContexts;
            set 
            { 
                _workingContexts = value;
                DataStateChanged?.Invoke(nameof(EDataRefreshType.Contexts), this);
            }
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
        UserDisplayModel? IDataStateInternal.ChangedUserData { get; set; }
        UserSettingsDisplayModel? IDataStateInternal.ChangedUserSettingsData { get; set; }
        List<TaskDisplayModel> IDataStateInternal.ChangedTaskData { get; set; } = new();
        List<ProjectDisplayModel> IDataStateInternal.ChangedProjectData { get; set; } = new();
        List<ContextDisplayModel> IDataStateInternal.ChangedContextData { get; set; } = new();

        // tracking pre-push local insertions
        int IDataStateInternal.TempTaskId { get; set; } = 0;
        int IDataStateInternal.TempProjectId { get; set; } = 0;
        int IDataStateInternal.TempContextId { get; set; } = 0;
    }
}
