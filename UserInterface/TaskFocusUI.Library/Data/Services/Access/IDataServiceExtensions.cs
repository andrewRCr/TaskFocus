using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Schema;
using TaskFocusUI.Library.Data.State;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Services.Access
{
    internal static class IDataServiceExtensions
    {
        public static void HandleIndexShiftsOnTaskDeletion(this IDataService iface, TaskDisplayModel task)
        {
            ((IDataServiceInternal)iface).HandleIndexShiftsOnTaskDeletion(task);
        }

        public static void ShiftCollectionOrderIndices<T>(this IDataService iface,
                                                          T collectionDisplayModel,
                                                          List<T> collectionSource) where T : ICollectionDisplayModel
        {
            ((IDataServiceInternal)iface).ShiftCollectionOrderIndices(collectionDisplayModel, collectionSource);
        }

        public static void UpdateAllWorkingDataAfterPull(this IDataService iface)
        {
            ((IDataServiceInternal)iface).UpdateAllWorkingDataAfterPull();
        }

        public static void PerformCompletedTaskCleanup(this IDataService iface)
        {
            ((IDataServiceInternal)iface).PerformCompletedTaskCleanup();
        }
    }
}
