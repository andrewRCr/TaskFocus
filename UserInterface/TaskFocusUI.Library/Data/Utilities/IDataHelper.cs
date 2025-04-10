using System.Collections.Generic;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Utilities
{
    public interface IDataHelper
    {
        ProjectDisplayModel? FocusedProject { get; internal set; }
        List<TaskDisplayModel>? FocusedProjectTasks { get; internal set; }
        ContextDisplayModel? FocusedContext { get; internal set; }
        List<TaskDisplayModel>? FocusedContextTasks { get; internal set; }

        TaskDataCompareResult HasTaskDataChanged(TaskDisplayModel workingTask);
        CollectionDataCompareResult HasProjectDataChanged(ProjectDisplayModel workingProject);
        CollectionDataCompareResult HasContextDataChanged(ContextDisplayModel workingContext);
        bool HasSettingsDataChanged(UserSettingsDisplayModel workingSettings);
        bool HasUserDataChanged(UserDisplayModel workingUser);

        bool IsTaskDueOrOverDue(TaskDisplayModel workingTask);
        bool IsNewProjectNameUnique(string proposedProjectName);
        bool IsNewContextNameUnique(string proposedContextName);
        bool IsUpdatedProjectNameUnique(ProjectDisplayModel updatedDisplayProject);
        bool IsUpdatedContextNameUnique(ContextDisplayModel updatedDisplayContext);

        DataSyncResult CombineSyncResults(DataSyncResult resultA, DataSyncResult resultB);
        bool SyncChangesDetected(DataSyncResult result);
    }
}