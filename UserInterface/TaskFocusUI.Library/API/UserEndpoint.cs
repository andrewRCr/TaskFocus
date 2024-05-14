using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.API
{
    public class UserEndpoint : IUserEndpoint
    {
        private readonly IAPIHelper _apiHelper;

        public UserEndpoint(IAPIHelper apiHelper)
        {
            _apiHelper = apiHelper;
        }

        public async Task SendPasswordResetEmail(UserModel userModel)
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.PostAsJsonAsync("/api/user/SendPasswordResetEmail", userModel))
            {
                if (response.IsSuccessStatusCode)
                {
                    // TODO - log successful call ?
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
                    // TODO - log successful call ?
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task SendTestEmailToUser()
        {
            using (HttpResponseMessage response = await _apiHelper.APIClient.GetAsync("/api/user/SendTestEmailToUser"))
            {
                if (response.IsSuccessStatusCode)
                {
                    // TODO - log successful call ?
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
                    // TODO - log successful update call ?
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
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(response.ReasonPhrase);
                }
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
                updatedUserModel.Roles
            };

            using (HttpResponseMessage response = await _apiHelper.APIClient.PutAsJsonAsync("/api/User/UpdateName", data))
            {
                if (response.IsSuccessStatusCode)
                {
                    // TODO - log successful update call ?
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public async Task RequestUpdateEmail(UserModel updatedUserModel)
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
                    // TODO - log successful update call ?
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
                    // TODO - log successful update call ?
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
                    // TODO - log successful update call ?
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
                    // TODO - log successful update call ?
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
                    // TODO - log successful update call ?
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }
    }
}
