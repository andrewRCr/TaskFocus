using Microsoft.Extensions.Logging;
using System;
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

        public async Task SendTestEmailToUser()
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.GetAsync("/api/user/SendTestEmailToUser"))
            {
                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation("API: Test email sent to default dev email address.");
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task SendPasswordResetEmail(UserModel userModel)
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.PostAsJsonAsync("/api/user/SendPasswordResetEmail", userModel))
            {
                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation($"API: Password reset email succesfully sent to {userModel.Email}.");
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task SendPasswordChangeSuccessEmail(UserModel userModel)
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.PostAsJsonAsync("/api/user/SendPasswordChangeSuccessEmail", userModel))
            {
                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation($"API: Password changed success confirm email succesfully sent to {userModel.Email}.");
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task SendEmailConfirmationLink(UserModel userModel)
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.PostAsJsonAsync("/api/user/SendEmailConfirmationLink", userModel))
            {
                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation($"API: Account confirmation link email succesfully sent to {userModel.Email}.");
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task<bool> CheckUserEmailConfirmed(UserModel userModel)
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.PostAsJsonAsync("/api/user/CheckUserEmailConfirmed", userModel))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<bool>();
                    return result;
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task<bool> CheckUserExists(UserModel userModel)
        {
            if (string.IsNullOrWhiteSpace(userModel.Email)) { return false; }

            using (HttpResponseMessage response = await _apiHelper.APIClient.PostAsJsonAsync("/api/user/CheckUserExists", userModel))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<bool>();
                    return result;
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task<bool> CheckPasswordValid(CheckPasswordModel checkPasswordModel)
        {
            if (string.IsNullOrWhiteSpace(checkPasswordModel.Email) || 
                string.IsNullOrWhiteSpace(checkPasswordModel.Password)) { return false; }

            using (HttpResponseMessage response = await _apiHelper.APIClient.PostAsJsonAsync("/api/user/CheckPasswordValid", checkPasswordModel))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<bool>();
                    return result;
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task<UserModel> GetUserById()
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.GetAsync("/api/user/Admin/GetUserById"))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<UserModel>();
                    return result;
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task<List<UserModel>> GetAllUsers()
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.GetAsync("/api/user/Admin/GetAllUsers"))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<List<UserModel>>();
                    return result;
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task<UserModel> GetCurrentUserData()
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.GetAsync("/api/user/GetCurrentUser"))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<UserModel>();
                    return result;
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task<UserSettingsModel> GetCurrentUserSettings()
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.GetAsync("/api/user/GetCurrentUserSettings"))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<UserSettingsModel>();
                    return result;
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task UpdateUserSettings(UserSettingsModel updatedSettings)
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.PutAsJsonAsync("/api/user/put/settings", updatedSettings))
            {
                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation("API: User UpdateUserSettings request processed successfully.");
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task CreateUser(CreateUserModel userModel)
        {
            var data = new { 
                userModel.FirstName, 
                userModel.LastName,
                userModel.Email,
                userModel.Password,
            };

            using (HttpResponseMessage response = await _apiHelper.APIClient.PostAsJsonAsync("/api/User/Register", data))
            {
                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation($"API: User {userModel.Email} created successfully.");
                }
                else { throw new Exception(response.ReasonPhrase); }
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

            using (HttpResponseMessage response = await _apiHelper.APIClient.PutAsJsonAsync("/api/User/UpdateName", data))
            {
                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation($"API: User name updated to {updatedUserModel.FirstName} {updatedUserModel.LastName} successfully.");
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

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

            using (HttpResponseMessage response = await _apiHelper.APIClient.PutAsJsonAsync("/api/User/RequestUpdateEmail", data))
            {
                if (response.IsSuccessStatusCode)
                {               
                    _logger?.LogInformation($"API: User RequestUpdateEmail call processed successfully.");
                    return true;
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task UpdatePassword(CreateUserModel updatedUserModel)
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.PutAsJsonAsync("/api/User/UpdatePassword", updatedUserModel))
            {
                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation("API: User UpdatePassword request processed successfully.");
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task ResetPassword(ResetPasswordModel resetPasswordModel)
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.PostAsJsonAsync("/api/User/ResetPassword", resetPasswordModel))
            {
                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation("API: User ResetPassword request processed successfully.");
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task ConfirmEmail(ConfirmEmailModel confirmEmailModel)
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.PostAsJsonAsync("/api/User/ConfirmEmail", confirmEmailModel))
            {
                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation("API: User ConfirmEmail request processed successfully.");
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task ConfirmUpdatedEmail(ConfirmUpdatedEmailModel confirmUpdatedEmailModel)
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.PostAsJsonAsync("/api/User/ConfirmUpdatedEmail", confirmUpdatedEmailModel))
            {
                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation("API: User ConfirmUpdatedEmail request processed successfully.");
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }
    }
}
