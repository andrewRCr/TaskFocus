using System.Collections.Generic;
using System;
using TaskFocusUI.Library.Models;
using System.Collections.ObjectModel;

namespace TaskFocusUI.Library.Data.State
{
    public delegate void DataStateChangedHandler(string propertyName, IDataState dataState);

    public interface IDataState
    {
        //UserDisplayModel? CurrentUser { get; set; }
        //UserSettingsDisplayModel? UserSettings { get; set; }
        //List<TaskDisplayModel>? Tasks { get; set; }
        //List<TaskDisplayModel>? WorkingTasks { get; set; }
        //List<ProjectDisplayModel>? Projects { get; set; }
        //List<ProjectDisplayModel>? WorkingProjects { get; set; }
        //List<ContextDisplayModel>? Contexts { get; set; }
        //List<ContextDisplayModel>? WorkingContexts { get; set; }

        DateTimeOffset LastSync { get; set; }
        UserDisplayModel? ChangedUserData { get; set; }
        UserSettingsDisplayModel? ChangedUserSettingsData { get; set; }
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