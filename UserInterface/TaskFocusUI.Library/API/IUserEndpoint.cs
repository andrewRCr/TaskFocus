using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.API
{
    public interface IUserEndpoint
    {
        Task CreateUser(CreateUserModel userModel);
        Task<List<UserModel>> GetAllUsers();
        Task<UserModel> GetCurrentUserData();
        Task<UserSettingsModel> GetCurrentUserSettings();
        Task SendTestEmailToUser();
        Task UpdatePassword(CreateUserModel userModel);
        Task UpdateUser(UserModel updatedUserModel);
        Task UpdateUserSettings(UserSettingsModel updatedSettings);
    }
}