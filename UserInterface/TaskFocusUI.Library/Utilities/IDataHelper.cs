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
        ProjectDisplayModel FocusedProject { get; set; }
        List<TaskDisplayModel> FocusedProjectTasks { get; set; }

        bool HasContextDataChanged(ContextModel frontEndContext);
        bool HasProjectDataChanged(ProjectModel frontEndProject);
        bool HasSettingsDataChanged(UserSettingsModel frontEndSettings);
        bool HasTaskContextNameChanged(TaskModel frontEndTask);
        bool HasTaskDataChanged(TaskModel frontEndTask);
        bool HasTaskProjectNameChanged(TaskModel frontEndTask);
        bool IsNewContextNameUnique(string proposedContextName);
        bool IsNewProjectNameUnique(string proposedProjectName);
        bool IsTaskDueOrOverDue(TaskModel frontEndTask);
        bool IsUpdatedContextNameUnique(ContextModel updatedFrontEndContext);
        bool IsUpdatedProjectNameUnique(ProjectModel updatedFrontEndProject);
    }
}