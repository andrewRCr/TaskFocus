namespace TaskFocusDesktop.EventModels
{
    public class UnconfirmedUpdatedEmailNotifyEvent
    {
        public string Email { get; set; }

        public UnconfirmedUpdatedEmailNotifyEvent(string emailAddress)
        {          
            Email = emailAddress;
        }
    }
}
