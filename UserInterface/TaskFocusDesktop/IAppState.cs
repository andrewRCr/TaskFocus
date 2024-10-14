namespace TaskFocusDesktop
{
    public interface IAppState
    {
        bool ShouldAutoLogin { get; set; }
        bool IsAuthenticated { get; set; }

        event AppStateChangedHandler AppStateChanged;
    }
}