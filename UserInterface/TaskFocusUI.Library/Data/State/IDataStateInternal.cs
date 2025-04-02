using System.Collections.Generic;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.State
{
    internal interface IDataStateInternal
    {
        UserDisplayModel? CurrentUser { get; set; }
        UserDisplayModel? WorkingCurrentUser { get; set; }

        UserSettingsDisplayModel? UserSettings { get; set; }
        UserSettingsDisplayModel? WorkingUserSettings { get; set; }

        List<TaskDisplayModel>? Tasks { get; set; }
        List<TaskDisplayModel>? WorkingTasks { get; set; }

        List<ProjectDisplayModel>? Projects { get; set; }
        List<ProjectDisplayModel>? WorkingProjects { get; set; }

        List<ContextDisplayModel>? Contexts { get; set; }
        List<ContextDisplayModel>? WorkingContexts { get; set; }

        bool IsDataLoaded();
    }
}
