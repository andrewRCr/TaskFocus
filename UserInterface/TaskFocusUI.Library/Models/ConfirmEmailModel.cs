using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TaskFocusUI.Library.Models
{
    public class ConfirmEmailModel
    {
        public string Email { get; set; }
        public string Token { get; set; }
    }
}
