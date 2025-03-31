using System.Collections.Generic;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Utilities
{
    public interface IDataHelper
    {
        //List<TaskModel>? TasksLastFetch { get; set; }
        //List<ProjectModel>? ProjectsLastFetch { get; set; }
        //List<ContextModel>? ContextsLastFetch { get; set; }
        UserSettingsModel? UserSettingsLastFetch { get; set; }

        ProjectDisplayModel? FocusedProject { get; set; }
        List<TaskDisplayModel>? FocusedProjectTasks { get; set; }
        ContextDisplayModel? FocusedContext { get; set; }
        List<TaskDisplayModel>? FocusedContextTasks { get; set; }

        TaskDataCompareResult HasTaskDataChanged(TaskDisplayModel displayTask);
        ProjectDataCompareResult HasProjectDataChanged(ProjectDisplayModel displayProject);
        ContextDataCompareResult HasContextDataChanged(ContextDisplayModel displayContext);
        bool HasSettingsDataChanged(UserSettingsModel displaySettings);

        bool IsTaskDueOrOverDue(TaskDisplayModel frontEndTask);
        bool IsNewProjectNameUnique(string proposedProjectName);
        bool IsNewContextNameUnique(string proposedContextName);
        bool IsUpdatedProjectNameUnique(ProjectDisplayModel updatedDisplayProject);
        bool IsUpdatedContextNameUnique(ContextDisplayModel updatedDisplayContext);
        DataSyncResult CombineSyncResults(DataSyncResult resultA, DataSyncResult resultB);
        bool SyncChangesDetected(DataSyncResult result);
    }
}