using System.Threading.Tasks;

namespace TaskFocusUI.Library.Utilities
{
    public interface IDataSyncService
    {
        Task InitSync();
        Task Sync();
    }
}