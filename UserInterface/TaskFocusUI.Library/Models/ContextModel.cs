using System;
using System.Collections.Generic;
using System.Text;

namespace TaskFocusUI.Library.Models
{
    public class ContextModel
    {
        public int? Id { get; set; }
        public string UserId { get; set; }
        public string ContextName { get; set; }
        public int? OrderIndex { get; set; }
        // for sync
        public DateTimeOffset ServerLastUpdated { get; set; }
        public DateTimeOffset ClientLastUpdated { get; set; }
        public DateTimeOffset? Deleted { get; set; }
    }
}
