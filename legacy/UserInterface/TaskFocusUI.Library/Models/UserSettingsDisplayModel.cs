using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace TaskFocusUI.Library.Models
{
    public class UserSettingsDisplayModel : INotifyPropertyChanged, ISyncableUserData
    {
        public string Id { get; set; }

        // for sync
        public ESyncableUserDataType DataType { get; } = ESyncableUserDataType.Settings;
        public DateTimeOffset ServerLastUpdated { get; set; }
        public DateTimeOffset ClientLastUpdated { get; set; }

        // user-editable properties
        // ====================
        private bool _cleanUpImmediately = false;
        public bool CleanUpImmediately
        {
            get { return _cleanUpImmediately; }
            set
            {
                _cleanUpImmediately = value;
                CallPropertyChanged(nameof(CleanUpImmediately));
            }
        }

        private int _cleanUpDelayDays = 7;
        public int CleanUpDelayDays
        {
            get { return _cleanUpDelayDays; }
            set
            {
                _cleanUpDelayDays = value;
                CallPropertyChanged(nameof(CleanUpDelayDays));
            }
        }

        private int _deleteDelayDays = 30;
        public int DeleteDelayDays
        {
            get { return _deleteDelayDays; }
            set
            {
                _deleteDelayDays = value;
                CallPropertyChanged(nameof(DeleteDelayDays));
            }
        }

        public UserSettingsDisplayModel Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<UserSettingsDisplayModel>(serialized)!;
        }

        // for updating while maintaining references
        public void ValueAssign(UserSettingsDisplayModel source)
        {
            Id = source.Id;
            CleanUpImmediately = source.CleanUpImmediately;
            CleanUpDelayDays = source.CleanUpDelayDays;
            DeleteDelayDays = source.DeleteDelayDays;
            ServerLastUpdated = source.ServerLastUpdated;
            ClientLastUpdated = source.ClientLastUpdated;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void CallPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
