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
        bool IsTaskDueOrOverDue(TaskDisplayModel workingTask);

        CollectionDataCompareResult HasProjectDataChanged(ProjectDisplayModel workingProject);
        bool IsNewProjectNameUnique(string proposedProjectName);
        bool IsUpdatedProjectNameUnique(ProjectDisplayModel updatedDisplayProject);

        CollectionDataCompareResult HasContextDataChanged(ContextDisplayModel workingContext);
        bool IsNewContextNameUnique(string proposedContextName);
        bool IsUpdatedContextNameUnique(ContextDisplayModel updatedDisplayContext);

        bool HasSettingsDataChanged(UserSettingsDisplayModel workingSettings);
        bool HasUserDataChanged(UserDisplayModel workingUser);

        DataSyncResult CombineSyncResults(DataSyncResult resultA, DataSyncResult resultB);
        bool SyncChangesDetected(DataSyncResult result);
    }
}