using System;
using System.Collections.Generic;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library
{
    public class DataState : IDataState
    {
        public event DataStateChangedHandler DataStateChanged = default!;

        private UserModel? _currentUser;
        public UserModel? CurrentUser
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
            return Tasks != null && Projects != null && Contexts != null
                && UserSettings != null && CurrentUser != null;
        }
    }
}
