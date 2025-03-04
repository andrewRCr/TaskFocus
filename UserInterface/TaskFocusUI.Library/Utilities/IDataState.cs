using System.Collections.Generic;
using System;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library
{
    public delegate void DataStateChangedHandler(String propertyName, IDataState dataState);

    public interface IDataState
    {
        List<ContextDisplayModel>? Contexts { get; set; }
        UserDisplayModel? CurrentUser { get; set; }
        List<ProjectDisplayModel>? Projects { get; set; }
        List<TaskDisplayModel>? Tasks { get; set; }
        UserSettingsDisplayModel? UserSettings { get; set; }

        DateTimeOffset LastSync { get; set; }
        List<UserModel> ChangedUserData { get; set; }
        List<UserSettingsModel> ChangedUserSettingsData { get; set; }
        List<TaskModel> ChangedTaskData { get; set; }
        List<ProjectModel> ChangedProjectData { get; set; }
        List<ContextModel> ChangedContextData { get; set; }

        event DataStateChangedHandler DataStateChanged;

        bool IsDataLoaded();
    }
}