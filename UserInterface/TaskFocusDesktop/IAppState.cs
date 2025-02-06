namespace TaskFocusDesktop
{
    public interface IAppState
    {
        bool ShouldAutoLogin { get; set; }
        bool IsAuthenticated { get; set; }
        string AlertMessage { get; set; }
        double AppWindowHeight { get; set; }

        event AppStateChangedHandler AppStateChanged;
    }
}