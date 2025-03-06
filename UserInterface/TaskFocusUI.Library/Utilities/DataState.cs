using System;
using System.Collections.Generic;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library
{
    public class DataState : IDataState
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
        public List<TaskDisplayModel>? Tasks
        {
            get { return _tasks; }
            set
            {
                _tasks = value;
                DataStateChanged?.Invoke(nameof(Tasks), this);
            }
        }

        private List<ProjectDisplayModel>? _projects;
        public List<ProjectDisplayModel>? Projects
        {
            get { return _projects; }
            set
            {
                _projects = value;
                DataStateChanged?.Invoke(nameof(Projects), this);
            }
        }

        private List<ContextDisplayModel>? _contexts;
        public List<ContextDisplayModel>? Contexts
        {
            get { return _contexts; }
            set
            {
                _contexts = value;
                DataStateChanged?.Invoke(nameof(Contexts), this);
            }
        }

        public bool IsDataLoaded()
        {
            return CurrentUser != null && UserSettings != null &&
                Tasks != null && Projects != null && Contexts != null;       
        }

        public List<UserModel> ChangedUserData { get; set; } = new();
        public List<UserSettingsModel> ChangedUserSettingsData { get; set; } = new();
        public List<TaskDisplayModel> ChangedTaskData { get; set; } = new();
        public List<ProjectModel> ChangedProjectData { get; set; } = new();
        public List<ContextModel> ChangedContextData { get; set; } = new();

        public int TempTaskId { get; set; } = 0;
    }
}
