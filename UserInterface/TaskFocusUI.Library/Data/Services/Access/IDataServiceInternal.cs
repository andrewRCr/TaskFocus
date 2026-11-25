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
        void PerformTodayTaskCleanup();
    }
}
