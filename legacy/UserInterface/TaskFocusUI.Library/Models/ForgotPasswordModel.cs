using System.ComponentModel.DataAnnotations;

namespace TaskFocusUI.Library.Models
{
    public class ForgotPasswordModel
    {
        [Required]
        [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
        public string Email { get; set; }
    }
}
