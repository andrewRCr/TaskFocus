namespace TaskFocusDesktop.EventModels
{
    public class FocusedContextChangedEvent
    {
        public int NewFocusedContextId { get; set; }
        public string NewFocusedContextName { get; set; }

        public FocusedContextChangedEvent(int newFocusedContextId, string newFocusedContextName)
        {
            NewFocusedContextId = newFocusedContextId;
            NewFocusedContextName = newFocusedContextName;
        }
    }
}
