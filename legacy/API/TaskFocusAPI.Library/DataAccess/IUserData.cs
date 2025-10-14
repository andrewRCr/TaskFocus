using System.Collections.Generic;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Library.DataAccess
{
    public interface IUserData
    {
        void CreateUser(UserModel user);
        List<UserModel> GetUserById(string id);
        UserSettingsModel GetUserSettingsById(string id);
        void UpdateSettingsData(UserSettingsModel frontEndSettings);
        void UpdateUser(UserModel user);
    }
}