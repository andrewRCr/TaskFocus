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
                userModel.EmailAddress,
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
    }
}
