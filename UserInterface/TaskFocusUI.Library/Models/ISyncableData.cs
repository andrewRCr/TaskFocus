using System;

namespace TaskFocusUI.Library.Models
{
    public enum ESyncableDataType
    {
        Task,
        Project,
        Context
    }

    public interface ISyncableData
    {
        public int? Id { get; set; }
        public int? TempLocalId { get; set; }
        public ESyncableDataType DataType { get; }

        // for sync
        public DateTimeOffset ServerLastUpdated { get; set; }
        public DateTimeOffset ClientLastUpdated { get; set; }
        public DateTimeOffset? Deleted { get; set; }
    }
}
