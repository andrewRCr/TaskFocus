namespace TaskFocusDesktop.EventModels
{
    public class AppWindowHeightChangedEvent
    {
        public double NewAppWindowHeight { get; set; }

        public AppWindowHeightChangedEvent(double newAppWindowHeight)
        {
            NewAppWindowHeight = newAppWindowHeight;
        }
    }
}
