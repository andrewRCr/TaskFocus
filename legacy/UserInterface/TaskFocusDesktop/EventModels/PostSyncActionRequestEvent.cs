namespace TaskFocusDesktop.EventModels
{
    public class PostSyncActionRequestEvent
    {
        public EPostSyncAction RequestedAction { get; set; }

        public PostSyncActionRequestEvent(EPostSyncAction action)
        {
            RequestedAction = action;
        }
    }
}
