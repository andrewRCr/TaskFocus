using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.API
{
    public class APIHelper : IAPIHelper
    {
        private HttpClient _apiClient;
        private readonly ILoggedInUserModel _loggedInUser;
        private readonly IConfiguration _config;

        public HttpClient APIClient
        {
            get { return _apiClient; } 
        }

        public APIHelper(ILoggedInUserModel loggedInUser, IConfiguration config)
        {
            _loggedInUser = loggedInUser;
            _config = config;
            InitializeClient();
        }

        private void InitializeClient()
        {
            string api = _config.GetValue<string>("api");

            _apiClient = new HttpClient();
            _apiClient.BaseAddress = new Uri(api);
            _apiClient.DefaultRequestHeaders.Accept.Clear();
            _apiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<AuthenticatedUser> AuthenticateAsync(string username, string password)
        {
            var data = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            });

            using (HttpResponseMessage response = await _apiClient.PostAsync("/token", data))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<AuthenticatedUser>();
                    return result;
                }
                else { throw new Exception(response.ReasonPhrase); }
            }
        }

        public string GetLoggedInUserId()
        {
            if (_loggedInUser == null) { return null; }

            return _loggedInUser.Id;
        }

        public async Task GetLoggedInUserInfoAsync(string token)
        {
            _apiClient.DefaultRequestHeaders.Clear();
            _apiClient.DefaultRequestHeaders.Accept.Clear();
            _apiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _apiClient.DefaultRequestHeaders.Add("Authorization", $"Bearer { token }");

            using (HttpResponseMessage response = await _apiClient.GetAsync("/api/user/getcurrentuser"))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<LoggedInUserModel>();
                    _loggedInUser.Token = token;
                    _loggedInUser.Id = result.Id;
                    _loggedInUser.Email = result.Email;
                    _loggedInUser.FirstName = result.FirstName;
                    _loggedInUser.LastName = result.LastName;
                    _loggedInUser.CreatedDate = result.CreatedDate;
                }
                else { throw new Exception(response.ReasonPhrase);}
            }
        }

        public void LogOutUser()
        {
            _apiClient.DefaultRequestHeaders.Clear();
        }
    }
}
