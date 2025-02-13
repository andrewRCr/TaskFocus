namespace TaskFocusAPI.Library.Models
{
    public class UserSettingsModel
    {
        public string Id { get; set; }
        public bool CleanUpImmediately { get; set; } = false;
        public int CleanUpDelayDays { get; set; } = 7; 
        public int DeleteDelayDays { get; set; } = 30;
    }
}