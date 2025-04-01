using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskFocusUI.Library.Models
{
    public class UserModel : ISyncableUserData
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Dictionary<string, string> Roles { get; set; } = new Dictionary<string, string>();

        // for sync
        public ESyncableUserDataType DataType { get; } = ESyncableUserDataType.User;
        public DateTimeOffset ServerLastUpdated { get; set; }
        public DateTimeOffset ClientLastUpdated { get; set; }

        public UserModel Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<UserModel>(serialized)!;
        }

        public string RoleList
        {
            get 
            {
                return string.Join(", ", Roles.Select(x => x.Value));
            }
        }
    }
}
