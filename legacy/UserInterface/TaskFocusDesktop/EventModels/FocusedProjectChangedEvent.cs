namespace TaskFocusDesktop.EventModels
{
    public class FocusedProjectChangedEvent
    {
        public int NewFocusedProjectId { get; set; }
        public string NewFocusedProjectName { get; set; }

        public FocusedProjectChangedEvent(int newFocusedProjectId, string newFocusedProjectName)
        {
            NewFocusedProjectId = newFocusedProjectId;
            NewFocusedProjectName = newFocusedProjectName;
        }
    }
}
