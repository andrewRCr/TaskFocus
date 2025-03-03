using System;

namespace TaskFocusUI.Library.Models
{
    public class ColumnChange
    {
        public ColumnChange()
        {
            ChangedOn = DateTimeOffset.Now;
        }
        public string Column { get; set; }
        public DateTimeOffset ChangedOn { get; private set; }
    }
}
