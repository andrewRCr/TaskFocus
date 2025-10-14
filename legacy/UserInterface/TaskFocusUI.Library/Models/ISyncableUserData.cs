using System;

namespace TaskFocusUI.Library.Models
{
    public enum ESyncableUserDataType
    {
        User,
        Settings
    }

    public interface ISyncableUserData
    {
        public string? Id { get; set; }
        public ESyncableUserDataType DataType { get; }

        // for sync
        public DateTimeOffset ServerLastUpdated { get; set; }
        public DateTimeOffset ClientLastUpdated { get; set; }
    }
    
}
