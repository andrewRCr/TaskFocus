using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TaskFocusUI.Library.Models
{
    public class ForgotPasswordModel
    {
        [Required]
        [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
        public string Email { get; set; }
    }
}
