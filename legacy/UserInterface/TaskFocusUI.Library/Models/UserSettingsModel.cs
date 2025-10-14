using System;

namespace TaskFocusUI.Library.Models
{
    public class UserSettingsModel : ISyncableUserData
    {
        public string Id { get; set; }
        public bool CleanUpImmediately { get; set; } = false;
        public int CleanUpDelayDays { get; set; } = 7;
        public int DeleteDelayDays { get; set; } = 30;
        // for sync
        public ESyncableUserDataType DataType { get; } = ESyncableUserDataType.Settings;
        public DateTimeOffset ServerLastUpdated { get; set; }
        public DateTimeOffset ClientLastUpdated { get; set; }
    }
}
