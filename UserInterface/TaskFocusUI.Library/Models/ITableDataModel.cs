using System;

namespace TaskFocusUI.Library.Models
{
    public interface ITableDataModel
    {
        public string Id { get; set; }

        // for sync
        public DateTimeOffset LastUpdated { get; set; }
        public DateTimeOffset ClientLastUpdated { get; set; }
        public DateTimeOffset? Deleted { get; set; }
        public string RowChanges { get; set; }
    }
}
