using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Schema;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Services.Access
{
    internal static class IDataServiceExtensions
    {
        public static void HandleIndexShiftsOnTaskDeletion(this IDataService iface, TaskDisplayModel task)
        {
            iface.HandleIndexShiftsOnTaskDeletion(task);
        }

        public static void ShiftCollectionOrderIndices<T>(this IDataService iface,
                                                          T collectionDisplayModel,
                                                          List<T> collectionSource) where T : ICollectionDisplayModel
        {
            iface.ShiftCollectionOrderIndices(collectionDisplayModel, collectionSource);
        }
    }
}
