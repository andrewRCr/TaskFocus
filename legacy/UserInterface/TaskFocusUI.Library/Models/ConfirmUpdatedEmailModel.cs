namespace TaskFocusUI.Library.Models
{
    public class ConfirmUpdatedEmailModel
    {
        public string OldEmail { get; set; }
        public string NewEmail { get; set; }
        public string Token { get; set; }
    }
}
