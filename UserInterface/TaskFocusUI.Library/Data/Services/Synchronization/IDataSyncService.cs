using System.Threading.Tasks;

namespace TaskFocusUI.Library.Data.Services.Synchronization
{
    public interface IDataSyncService
    {
        Task InitSync();
        Task Sync();
    }
}