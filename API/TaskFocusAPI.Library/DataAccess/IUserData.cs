using System.Collections.Generic;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Library.DataAccess
{
    public interface IUserData
    {
        void CreateUser(UserModel user);
        List<UserModel> GetUserById(string id);
        UserSettingsModel GetUserSettingsById(string id);
        bool UpdateSettingsData(UserSettingsModel frontEndSettings, string userId);
        void UpdateUser(UserModel user);
        bool UpdateUserName(UserModel user);
    }
}
