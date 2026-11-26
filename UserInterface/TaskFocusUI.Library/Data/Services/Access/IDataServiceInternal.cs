using System;
using System.Collections.Generic;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Services.Access
{
    internal interface IDataServiceInternal
    {
        void ShiftCollectionOrderIndices<T>(T collectionDisplayModel, List<T> collectionSource) where T : ICollectionDisplayModel;
        void UpdateAllWorkingDataAfterPull();
        void PerformCompletedTaskCleanup();
        System.Threading.Tasks.Task<TaskFocusUI.Library.Data.Utilities.DataSyncResult> DeleteExpiredTasks();
        void PerformTodayTaskCleanup();
    }
}
