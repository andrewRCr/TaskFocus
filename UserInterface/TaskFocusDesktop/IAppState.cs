using TaskFocusUI.Library.Data.Services.Synchronization;

namespace TaskFocusDesktop
{
    public interface IAppState
    {
        bool ShouldAutoLogin { get; set; }
        bool IsAuthenticated { get; set; }
        EPostSyncAction PendingPostSyncAction { get; set; }

        string AlertMessage { get; set; }
        double AppWindowHeight { get; set; }

        event AppStateChangedHandler AppStateChanged;
    }
}