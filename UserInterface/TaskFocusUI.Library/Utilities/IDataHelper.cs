using System.Collections.Generic;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Utilities
{
    public interface IDataHelper
    {
        List<TaskModel>? TasksLastFetch { get; set; }
        List<ProjectModel>? ProjectsLastFetch { get; set; }
        List<ContextModel>? ContextsLastFetch { get; set; }
        UserSettingsModel? UserSettingsLastFetch { get; set; }

        ProjectDisplayModel? FocusedProject { get; set; }
        List<TaskDisplayModel>? FocusedProjectTasks { get; set; }
        ContextDisplayModel? FocusedContext { get; set; }
        List<TaskDisplayModel>? FocusedContextTasks { get; set; }

        TaskDataCompareResult HasTaskDataChanged(TaskDisplayModel displayTask);
        bool HasProjectDataChanged(ProjectModel frontEndProject);
        bool HasContextDataChanged(ContextModel frontEndContext);
        bool HasSettingsDataChanged(UserSettingsModel frontEndSettings);

        bool IsTaskDueOrOverDue(TaskModel frontEndTask);
        bool IsNewProjectNameUnique(string proposedProjectName);
        bool IsNewContextNameUnique(string proposedContextName);
        bool IsUpdatedProjectNameUnique(ProjectModel updatedFrontEndProject);
        bool IsUpdatedContextNameUnique(ContextModel updatedFrontEndContext);
        DataSyncResult CombineSyncResults(DataSyncResult resultA, DataSyncResult resultB);
        bool SyncChangesDetected(DataSyncResult result);
    }
}