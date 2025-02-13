namespace TaskFocusDesktop.EventModels
{
    class UnconfirmedEmailNotifyEvent
    {
        public string Email { get; set; }

        public UnconfirmedEmailNotifyEvent(string emailAddress)
        {
            Email = emailAddress;
        }
    }
}
