using System.Collections.Generic;
using System;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library
{
    public delegate void DataStateChangedHandler(String propertyName, IDataState dataState);

    public interface IDataState
    {
        List<ContextDisplayModel>? Contexts { get; set; }
        UserModel? CurrentUser { get; set; }
        List<ProjectDisplayModel>? Projects { get; set; }
        List<TaskDisplayModel>? Tasks { get; set; }
        UserSettingsDisplayModel UserSettings { get; set; }

        event DataStateChangedHandler DataStateChanged;

        bool IsDataLoaded();
    }
}