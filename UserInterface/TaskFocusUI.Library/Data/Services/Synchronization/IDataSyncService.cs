using System;
using System.Threading.Tasks;

namespace TaskFocusUI.Library.Data.Services.Synchronization
{
    public interface IDataSyncService
    {
        Task<bool> TrySync(bool notifyOnCompletion = false, Action<Exception>? onFailure = null);
    }
}
