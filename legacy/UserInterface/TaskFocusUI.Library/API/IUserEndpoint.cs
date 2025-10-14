using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.API
{
    public interface IUserEndpoint
    {
        // user data - basic CRUD
        Task<UserModel> GetUserById();
        Task<List<UserModel>> GetAllUsers();
        Task<UserModel> GetCurrentUserData();
        Task CreateUser(CreateUserModel userModel);
        Task UpdateName(UserModel updatedUserModel);

        // update / confirm email
        Task<bool> RequestUpdateEmail(UserModel updatedUserModel);
        Task ConfirmEmail(ConfirmEmailModel confirmEmailModel);
        Task ConfirmUpdatedEmail(ConfirmUpdatedEmailModel confirmUpdatedEmailModel);

        // update / reset password
        Task UpdatePassword(CreateUserModel userModel);
        Task ResetPassword(ResetPasswordModel resetPasswordModel);

        // send account related emails
        Task SendTestEmailToUser();
        Task SendEmailConfirmationLink(UserModel userModel);
        Task SendPasswordResetEmail(UserModel userModel);
        Task SendPasswordChangeSuccessEmail(UserModel userModel);

        // helper methods
        Task<bool> CheckUserExists(UserModel userModel);
        Task<bool> CheckUserEmailConfirmed(UserModel userModel);
        Task<bool> CheckPasswordValid(CheckPasswordModel checkPasswordModel);

        // user settings CRUD
        Task<UserSettingsModel> GetCurrentUserSettings();
        Task UpdateUserSettings(UserSettingsModel updatedSettings);
    }
}