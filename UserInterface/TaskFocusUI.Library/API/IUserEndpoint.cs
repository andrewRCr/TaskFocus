using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.API
{
    public interface IUserEndpoint
    {
        Task<bool> CheckUserEmailConfirmed(UserModel userModel);
        Task<bool> CheckUserExists(UserModel userModel);
        Task ConfirmEmail(ConfirmEmailModel confirmEmailModel);
        Task CreateUser(CreateUserModel userModel);
        Task<List<UserModel>> GetAllUsers();
        Task<UserModel> GetCurrentUserData();
        Task<UserSettingsModel> GetCurrentUserSettings();
        Task ResetPassword(ResetPasswordModel resetPasswordModel);
        Task SendEmailConfirmationLink(UserModel userModel);
        Task SendPasswordResetEmail(UserModel userModel);
        Task SendTestEmailToUser();
        Task UpdatePassword(CreateUserModel userModel);
        Task UpdateName(UserModel updatedUserModel);
        Task UpdateUserSettings(UserSettingsModel updatedSettings);
        Task<bool> RequestUpdateEmail(UserModel updatedUserModel);
        Task ConfirmUpdatedEmail(ConfirmUpdatedEmailModel confirmUpdatedEmailModel);
        Task<bool> CheckPasswordValid(CheckPasswordModel checkPasswordModel);
        Task SendPasswordChangeSuccessEmail(UserModel userModel);
    }
}