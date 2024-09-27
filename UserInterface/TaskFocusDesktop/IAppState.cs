namespace TaskFocusDesktop
{
    public interface IAppState
    {
        bool ShouldAutoLogin { get; set; }

        event AppStateChangedHandler AppStateChanged;
    }
}