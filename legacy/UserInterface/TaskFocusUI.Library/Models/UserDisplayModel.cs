using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace TaskFocusUI.Library.Models
{
    public class UserDisplayModel : INotifyPropertyChanged, ISyncableUserData
    {
        public string Id { get; set; }
        public string Email { get; set; }

        // for sync
        public ESyncableUserDataType DataType { get; } = ESyncableUserDataType.User;
        public DateTimeOffset ServerLastUpdated { get; set; }
        public DateTimeOffset ClientLastUpdated { get; set; }

        // user-editable properties
        // ====================
        private string? _firstName;
        public string? FirstName
        {
            get { return _firstName; }
            set
            {
                _firstName = value;
                CallPropertyChanged(nameof(FirstName));
            }
        }

        private string? _lastName;
        public string? LastName
        {
            get { return _lastName; }
            set
            {
                _lastName = value;
                CallPropertyChanged(nameof(LastName));
            }
        }

        public UserDisplayModel Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<UserDisplayModel>(serialized)!;
        }

        // for updating while maintaining references
        public void ValueAssign(UserDisplayModel source)
        {
            Id = source.Id;
            Email = source.Email;
            FirstName = source.FirstName;
            LastName = source.LastName;
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
