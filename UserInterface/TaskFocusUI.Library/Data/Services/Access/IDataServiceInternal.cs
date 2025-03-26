using System.Collections.Generic;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.Data.Services.Access
{
    internal interface IDataServiceInternal
    {
        void HandleIndexShiftsOnTaskDeletion(TaskDisplayModel task);
        void ShiftCollectionOrderIndices<T>(T collectionDisplayModel, List<T> collectionSource) where T : ICollectionDisplayModel;
        void UpdateAllWorkingDataAfterPull();
    }
}
