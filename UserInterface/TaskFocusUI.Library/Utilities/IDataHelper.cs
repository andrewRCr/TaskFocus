using System.Collections.Generic;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Utilities
{
    public interface IDataHelper
    {
        List<TaskModel> TasksLastFetch { get; set; }
        List<ProjectModel> ProjectsLastFetch { get; set; }
        List<ContextModel> ContextsLastFetch { get; set; }
        UserSettingsModel UserSettingsLastFetch { get; set; }

        bool HasContextDataChanged(ContextModel frontEndContext);
        bool HasProjectDataChanged(ProjectModel frontEndProject);
        bool HasSettingsDataChanged(UserSettingsModel frontEndSettings);
        bool HasTaskContextNameChanged(TaskModel frontEndTask);
        bool HasTaskDataChanged(TaskModel frontEndTask);
        bool HasTaskProjectNameChanged(TaskModel frontEndTask);
        bool IsTaskDueOrOverDue(TaskModel frontEndTask);
    }
}