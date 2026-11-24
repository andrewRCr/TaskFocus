using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.API
{
    public class UserEndpoint : IUserEndpoint
    {
        private readonly IAPIHelper _apiHelper;
        private readonly ILogger<UserEndpoint> _logger;

        public UserEndpoint(IAPIHelper apiHelper, ILogger<UserEndpoint> logger = null)
        {
            _apiHelper = apiHelper;
            _logger = logger;
        }

        // user data - basic CRUD
        // ====================

        public async Task<UserModel> GetUserById()
        {
            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.GetAsync("/api/user/Admin/GetUserById")))
            {
                var result = await response.Content.ReadAsAsync<UserModel>();
                return result;
            }
        }

        public async Task<List<UserModel>> GetAllUsers()
        {
            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.GetAsync("/api/user/Admin/GetAllUsers")))
            {
                var result = await response.Content.ReadAsAsync<List<UserModel>>();
                return result;
            }
        }

        public async Task<UserModel> GetCurrentUserData()
        {
            using (HttpResponseMessage response = await ApiRequest.SendAccountAsync(_apiHelper.APIClient.GetAsync("/api/user/GetCurrentUser")))
            {
                var result = await response.Content.ReadAsAsync<UserModel>();
                return result;
            }
        }

        public async Task CreateUser(CreateUserModel userModel)
        {
            var data = new
            {
                userModel.FirstName,
                userModel.LastName,
                userModel.Email,
                userModel.Password,
            };

            using (HttpResponseMessage response = await ApiRequest.SendAccountAsync(_apiHelper.APIClient.PostAsJsonAsync("/api/User/Register", data)))
            {
                _logger?.LogInformation($"API: User {userModel.Email} created successfully.");
            }
        }

        public async Task UpdateName(UserModel updatedUserModel)
        {
            var data = new
            {
                updatedUserModel.Id,
                updatedUserModel.FirstName,
                updatedUserModel.LastName,
                updatedUserModel.Email,
                updatedUserModel.Roles,
                updatedUserModel.ServerLastUpdated,
                updatedUserModel.ClientLastUpdated
            };

            using (HttpResponseMessage response = await ApiRequest.SendAccountAsync(_apiHelper.APIClient.PutAsJsonAsync("/api/User/UpdateName", data)))
            {
                _logger?.LogInformation($"API: User name updated to {updatedUserModel.FirstName} {updatedUserModel.LastName} successfully.");
            }
        }

        // update / confirm email
        // ====================

        public async Task<bool> RequestUpdateEmail(UserModel updatedUserModel)
        {
            var data = new
            {
                updatedUserModel.Id,
                updatedUserModel.FirstName,
                updatedUserModel.LastName,
                updatedUserModel.Email,
                updatedUserModel.Roles
            };

            using (HttpResponseMessage response = await ApiRequest.SendAccountAsync(_apiHelper.APIClient.PutAsJsonAsync("/api/User/RequestUpdateEmail", data)))
            {
                _logger?.LogInformation($"API: User RequestUpdateEmail call processed successfully.");
                return true;
            }
        }

        public async Task ConfirmEmail(ConfirmEmailModel confirmEmailModel)
        {
            using (HttpResponseMessage response = await ApiRequest.SendAccountAsync(_apiHelper.APIClient.PostAsJsonAsync("/api/User/ConfirmEmail", confirmEmailModel)))
            {
                _logger?.LogInformation("API: User ConfirmEmail request processed successfully.");
            }
        }

        public async Task ConfirmUpdatedEmail(ConfirmUpdatedEmailModel confirmUpdatedEmailModel)
        {
            using (HttpResponseMessage response = await ApiRequest.SendAccountAsync(_apiHelper.APIClient.PostAsJsonAsync("/api/User/ConfirmUpdatedEmail", confirmUpdatedEmailModel)))
            {
                _logger?.LogInformation("API: User ConfirmUpdatedEmail request processed successfully.");
            }
        }

        // update / reset password
        // ====================

        public async Task UpdatePassword(CreateUserModel updatedUserModel)
        {
            using (HttpResponseMessage response = await ApiRequest.SendAccountAsync(_apiHelper.APIClient.PutAsJsonAsync("/api/User/UpdatePassword?reportRejection=true", updatedUserModel)))
            {
                _logger?.LogInformation("API: User UpdatePassword request processed successfully.");
            }
        }

        public async Task ResetPassword(ResetPasswordModel resetPasswordModel)
        {
            using (HttpResponseMessage response = await ApiRequest.SendAccountAsync(_apiHelper.APIClient.PostAsJsonAsync("/api/User/ResetPassword", resetPasswordModel)))
            {
                _logger?.LogInformation("API: User ResetPassword request processed successfully.");
            }
        }

        // account related email senders
        // ====================

        public async Task SendTestEmailToUser()
        {
            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.GetAsync("/api/user/SendTestEmailToUser")))
            {
                _logger?.LogInformation("API: Test email sent to default dev email address.");
            }
        }

        public async Task SendEmailConfirmationLink(UserModel userModel)
        {
            using (HttpResponseMessage response = await ApiRequest.SendAccountAsync(_apiHelper.APIClient.PostAsJsonAsync("/api/user/SendEmailConfirmationLink", userModel)))
            {
                _logger?.LogInformation($"API: Account confirmation link email succesfully sent to {userModel.Email}.");
            }
        }

        public async Task SendPasswordResetEmail(UserModel userModel)
        {
            using (HttpResponseMessage response = await ApiRequest.SendAccountAsync(_apiHelper.APIClient.PostAsJsonAsync("/api/user/SendPasswordResetEmail", userModel)))
            {
                _logger?.LogInformation($"API: Password reset email succesfully sent to {userModel.Email}.");
            }
        }

        public async Task SendPasswordChangeSuccessEmail(UserModel userModel)
        {
            using (HttpResponseMessage response = await ApiRequest.SendAccountAsync(_apiHelper.APIClient.PostAsJsonAsync("/api/user/SendPasswordChangeSuccessEmail", userModel)))
            {
                _logger?.LogInformation($"API: Password changed success confirm email succesfully sent to {userModel.Email}.");
            }
        }

        // helper methods
        // ====================

        public async Task<bool> CheckUserExists(UserModel userModel)
        {
            if (string.IsNullOrWhiteSpace(userModel.Email)) { return false; }

            using (HttpResponseMessage response = await ApiRequest.SendAccountAsync(_apiHelper.APIClient.PostAsJsonAsync("/api/user/CheckUserExists", userModel)))
            {
                var result = await response.Content.ReadAsAsync<bool>();
                return result;
            }
        }

        public async Task<bool> CheckUserEmailConfirmed(UserModel userModel)
        {
            using (HttpResponseMessage response = await ApiRequest.SendAccountAsync(_apiHelper.APIClient.PostAsJsonAsync("/api/user/CheckUserEmailConfirmed", userModel)))
            {
                var result = await response.Content.ReadAsAsync<bool>();
                return result;
            }
        }

        public async Task<bool> CheckPasswordValid(CheckPasswordModel checkPasswordModel)
        {
            if (string.IsNullOrWhiteSpace(checkPasswordModel.Email) || 
                string.IsNullOrWhiteSpace(checkPasswordModel.Password)) { return false; }

            using (HttpResponseMessage response = await ApiRequest.SendAccountAsync(_apiHelper.APIClient.PostAsJsonAsync("/api/user/CheckPasswordValid", checkPasswordModel)))
            {
                var result = await response.Content.ReadAsAsync<bool>();
                return result;
            }
        }

        // user settings CRUD
        // ====================

        public async Task<UserSettingsModel> GetCurrentUserSettings()
        {
            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.GetAsync("/api/user/GetCurrentUserSettings")))
            {
                var result = await response.Content.ReadAsAsync<UserSettingsModel>();
                return result;
            }
        }

        public async Task UpdateUserSettings(UserSettingsModel updatedSettings)
        {
            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiHelper.APIClient.PutAsJsonAsync("/api/user/put/settings", updatedSettings)))
            {
                _logger?.LogInformation("API: User UpdateUserSettings request processed successfully.");
            }
        }
    }
}
