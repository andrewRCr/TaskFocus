using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Extensions;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Models;
using TaskFocusUI.Library.Utilities;
using static MudBlazor.CategoryTypes;

namespace TaskFocusWeb
{
    public delegate void DataStateChangedHandler(String propertyName, DataState dataState);

    public class DataState
    {
        public event DataStateChangedHandler DataStateChanged = default!;

        private UserModel _currentUser = default!;
        public UserModel CurrentUser
        {
            get { return _currentUser; }
            set
            {
                Console.WriteLine("DataState: CurrentUser changed!");
                _currentUser = value;
                DataStateChanged?.Invoke(nameof(CurrentUser), this);
            }
        }

        private UserSettingsDisplayModel _userSettings = default!;
        public UserSettingsDisplayModel UserSettings
        {
            get { return _userSettings; }
            set
            {
                Console.WriteLine("DataState: UserSettings changed!");
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
                Console.WriteLine("DataState: Tasks changed!");
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
                Console.WriteLine("DataState: Projects changed!");
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
                Console.WriteLine("DataState: Contexts changed!");
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
