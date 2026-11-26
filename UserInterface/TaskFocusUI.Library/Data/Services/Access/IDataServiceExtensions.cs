using System.Collections.Generic;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Services.Access
{
    internal static class IDataServiceExtensions
    {
        public static void ShiftCollectionOrderIndices<T>(this IDataService iface,
                                                          T collectionDisplayModel,
                                                          List<T> collectionSource) where T : ICollectionDisplayModel => 
            ((IDataServiceInternal)iface).ShiftCollectionOrderIndices(collectionDisplayModel, collectionSource);

        public static void UpdateAllWorkingDataAfterPull(this IDataService iface) => 
            ((IDataServiceInternal)iface).UpdateAllWorkingDataAfterPull();

        public static void PerformCompletedTaskCleanup(this IDataService iface) => 
            ((IDataServiceInternal)iface).PerformCompletedTaskCleanup();

        public static System.Threading.Tasks.Task<TaskFocusUI.Library.Data.Utilities.DataSyncResult> DeleteExpiredTasks(this IDataService iface) =>
            ((IDataServiceInternal)iface).DeleteExpiredTasks();

        public static void PerformTodayTaskCleanup(this IDataService iface) =>
            ((IDataServiceInternal)iface).PerformTodayTaskCleanup();
    }
}
