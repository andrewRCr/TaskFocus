using TaskFocusUI.Library.Data.Services.Synchronization;

namespace TaskFocusDesktop.EventModels
{
    public class PostSyncActionRequestEvent
    {
        public PostSyncActionRequestEvent(EPostSyncAction action)
        {
            RequestedAction = action;
        }

        public EPostSyncAction RequestedAction { get; set; }
    }
}
