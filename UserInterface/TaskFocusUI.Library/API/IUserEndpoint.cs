using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.API
{
    public interface IUserEndpoint
    {
        Task CreateUser(CreateUserModel userModel);
        Task<List<UserModel>> GetAllUsers();
        Task<UserSettingsModel> GetCurrentUserSettings();
    }
}