using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
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
            using (HttpResponseMessage response = await SendAccountRequest(_apiHelper.APIClient.GetAsync("/api/user/GetCurrentUser")))
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

            using (HttpResponseMessage response = await SendAccountRequest(_apiHelper.APIClient.PostAsJsonAsync("/api/User/Register", data)))
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

            using (HttpResponseMessage response = await SendAccountRequest(_apiHelper.APIClient.PutAsJsonAsync("/api/User/UpdateName", data)))
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

            using (HttpResponseMessage response = await SendAccountRequest(_apiHelper.APIClient.PutAsJsonAsync("/api/User/RequestUpdateEmail", data)))
            {
                _logger?.LogInformation($"API: User RequestUpdateEmail call processed successfully.");
                return true;
            }
        }

        public async Task ConfirmEmail(ConfirmEmailModel confirmEmailModel)
        {
            using (HttpResponseMessage response = await SendAccountRequest(_apiHelper.APIClient.PostAsJsonAsync("/api/User/ConfirmEmail", confirmEmailModel)))
            {
                _logger?.LogInformation("API: User ConfirmEmail request processed successfully.");
            }
        }

        public async Task ConfirmUpdatedEmail(ConfirmUpdatedEmailModel confirmUpdatedEmailModel)
        {
            using (HttpResponseMessage response = await SendAccountRequest(_apiHelper.APIClient.PostAsJsonAsync("/api/User/ConfirmUpdatedEmail", confirmUpdatedEmailModel)))
            {
                _logger?.LogInformation("API: User ConfirmUpdatedEmail request processed successfully.");
            }
        }

        // update / reset password
        // ====================

        public async Task UpdatePassword(CreateUserModel updatedUserModel)
        {
            using (HttpResponseMessage response = await SendAccountRequest(_apiHelper.APIClient.PutAsJsonAsync("/api/User/UpdatePassword?reportRejection=true", updatedUserModel)))
            {
                _logger?.LogInformation("API: User UpdatePassword request processed successfully.");
            }
        }

        public async Task ResetPassword(ResetPasswordModel resetPasswordModel)
        {
            using (HttpResponseMessage response = await SendAccountRequest(_apiHelper.APIClient.PostAsJsonAsync("/api/User/ResetPassword", resetPasswordModel)))
            {
                _logger?.LogInformation("API: User ResetPassword request processed successfully.");
            }
        }

        // account related email senders
        // ====================

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

        public async Task SendEmailConfirmationLink(UserModel userModel)
        {
            using (HttpResponseMessage response = await SendAccountRequest(_apiHelper.APIClient.PostAsJsonAsync("/api/user/SendEmailConfirmationLink", userModel)))
            {
                _logger?.LogInformation($"API: Account confirmation link email succesfully sent to {userModel.Email}.");
            }
        }

        public async Task SendPasswordResetEmail(UserModel userModel)
        {
            using (HttpResponseMessage response = await SendAccountRequest(_apiHelper.APIClient.PostAsJsonAsync("/api/user/SendPasswordResetEmail", userModel)))
            {
                _logger?.LogInformation($"API: Password reset email succesfully sent to {userModel.Email}.");
            }
        }

        public async Task SendPasswordChangeSuccessEmail(UserModel userModel)
        {
            using (HttpResponseMessage response = await SendAccountRequest(_apiHelper.APIClient.PostAsJsonAsync("/api/user/SendPasswordChangeSuccessEmail", userModel)))
            {
                _logger?.LogInformation($"API: Password changed success confirm email succesfully sent to {userModel.Email}.");
            }
        }

        // helper methods
        // ====================

        public async Task<bool> CheckUserExists(UserModel userModel)
        {
            if (string.IsNullOrWhiteSpace(userModel.Email)) { return false; }

            using (HttpResponseMessage response = await SendAccountRequest(_apiHelper.APIClient.PostAsJsonAsync("/api/user/CheckUserExists", userModel)))
            {
                var result = await response.Content.ReadAsAsync<bool>();
                return result;
            }
        }

        public async Task<bool> CheckUserEmailConfirmed(UserModel userModel)
        {
            using (HttpResponseMessage response = await SendAccountRequest(_apiHelper.APIClient.PostAsJsonAsync("/api/user/CheckUserEmailConfirmed", userModel)))
            {
                var result = await response.Content.ReadAsAsync<bool>();
                return result;
            }
        }

        public async Task<bool> CheckPasswordValid(CheckPasswordModel checkPasswordModel)
        {
            if (string.IsNullOrWhiteSpace(checkPasswordModel.Email) || 
                string.IsNullOrWhiteSpace(checkPasswordModel.Password)) { return false; }

            using (HttpResponseMessage response = await SendAccountRequest(_apiHelper.APIClient.PostAsJsonAsync("/api/user/CheckPasswordValid", checkPasswordModel)))
            {
                var result = await response.Content.ReadAsAsync<bool>();
                return result;
            }
        }

        // user settings CRUD
        // ====================

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

        private static async Task<HttpResponseMessage> SendAccountRequest(Task<HttpResponseMessage> request)
        {
            HttpResponseMessage response;
            try
            {
                response = await request;
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
            {
                throw new Exception("Could not connect to the account service. Check your connection and try again.");
            }

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            using (response)
            {
                // Server failures may contain diagnostics. Only read user-facing rejection details.
                var status = (int)response.StatusCode;
                string? message = status is >= 400 and < 500
                    ? await ReadRejectionMessage(response.Content)
                    : null;

                throw new Exception(message ?? (response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                        "This account request is not allowed. Please sign in to continue.",
                    HttpStatusCode.NotFound or HttpStatusCode.Gone =>
                        "This account link is invalid or has expired. Please request a new link.",
                    HttpStatusCode.TooManyRequests =>
                        "There have been too many attempts. Please wait and try again.",
                    _ when status is >= 400 and < 500 =>
                        "The account request was rejected. Check your details and try again.",
                    _ => "The account service is unavailable. Please try again later."
                }));
            }
        }

        private static async Task<string?> ReadRejectionMessage(HttpContent content)
        {
            try
            {
                using var body = await JsonDocument.ParseAsync(await content.ReadAsStreamAsync());
                var root = body.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                var errors = root;
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Name.Equals("message", StringComparison.OrdinalIgnoreCase) &&
                        property.Value.ValueKind == JsonValueKind.String &&
                        property.Value.GetString() is string message && !string.IsNullOrWhiteSpace(message))
                    {
                        return message;
                    }

                    if (property.NameEquals("errors") && property.Value.ValueKind == JsonValueKind.Object)
                    {
                        errors = property.Value;
                    }
                }

                foreach (var field in errors.EnumerateObject())
                {
                    if (field.Value.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    var messages = field.Value.EnumerateArray()
                        .Where(error => error.ValueKind == JsonValueKind.String)
                        .Select(error => error.GetString())
                        .Where(message => !string.IsNullOrWhiteSpace(message));
                    var text = string.Join(" ", messages);
                    if (text.Length > 0)
                    {
                        return text;
                    }
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or System.IO.IOException or HttpRequestException)
            {
                // Empty, non-JSON, invalid-text and interrupted responses use the fallback for their status.
            }

            return null;
        }
    }
}
