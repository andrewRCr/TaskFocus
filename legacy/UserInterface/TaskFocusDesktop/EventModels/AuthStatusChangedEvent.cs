namespace TaskFocusDesktop.EventModels
{
    public class AuthStatusChangedEvent
    {
        public bool NewAuthStatus { get; set; }

        public AuthStatusChangedEvent(bool newAuthStatus)
        {
            NewAuthStatus = newAuthStatus; 
        }
    }
}
