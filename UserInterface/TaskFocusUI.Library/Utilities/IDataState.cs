using System.Collections.Generic;
using System;
using TaskFocusUI.Library.Models;
using System.Collections.ObjectModel;

namespace TaskFocusUI.Library
{
    public delegate void DataStateChangedHandler(String propertyName, IDataState dataState);

    public interface IDataState
    {
        UserDisplayModel? CurrentUser { get; set; }
        UserSettingsDisplayModel? UserSettings { get; set; }
        List<TaskDisplayModel>? Tasks { get; set; }
        List<ProjectDisplayModel>? Projects { get; set; }
        List<ContextDisplayModel>? Contexts { get; set; }

        DateTimeOffset LastSync { get; set; }
        List<UserModel> ChangedUserData { get; set; }
        List<UserSettingsModel> ChangedUserSettingsData { get; set; }
        List<TaskDisplayModel> ChangedTaskData { get; set; }
        List<ProjectDisplayModel> ChangedProjectData { get; set; }
        List<ContextDisplayModel> ChangedContextData { get; set; }
        int TempTaskId { get; set; }
        int TempProjectId { get; set; }
        int TempContextId { get; set; }

        event DataStateChangedHandler DataStateChanged;

        void InvokeDataStateChanged(string propertyName);
        bool IsDataLoaded();
    }
}