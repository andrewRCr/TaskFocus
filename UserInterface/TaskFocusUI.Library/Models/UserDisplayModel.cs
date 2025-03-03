using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace TaskFocusUI.Library.Models
{
    public class UserDisplayModel : INotifyPropertyChanged
    {
        //public ClientUserModel() {}
        //public ClientUserModel(UserModel row)
        //{
        //    Id = row.Id;
        //    Email = row.Email;
        //    FirstName = row.FirstName;
        //    LastName = row.LastName;
        //    //CreatedDate = row.CreatedDate;
        //    //ClientLastUpdated = row.ClientLastUpdated;
        //    LastUpdated = row.LastUpdated;
        //    Deleted = row.Deleted;
        //}

        public string Id { get; set; }
        public string Email { get; set; }

        public DateTimeOffset ServerLastUpdated { get; set; }
        public DateTimeOffset ClientLastUpdated { get; set; }
        public DateTimeOffset? Deleted { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;
        public void CallPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
