using System;
using System.Collections.Generic;
using System.Text;

namespace TaskFocusUI.Library.Models
{
    public class UserSettingsModel
    {
        public string Id { get; set; }
        public bool CleanUpImmediately { get; set; } = false;
        public int CleanUpDelayDays { get; set; } = 30;
    }
}
