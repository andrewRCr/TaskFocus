using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.API
{
    public class APIHelper : IAPIHelper
    {
        private ILogger<APIHelper> _logger;
        private readonly ILoggedInUserModel _loggedInUser;
        private readonly IConfiguration _config;

        public APIHelper(ILoggedInUserModel loggedInUser, IConfiguration config, ILogger<APIHelper> logger = null)
        {
            _loggedInUser = loggedInUser;
            _config = config;
            _logger = logger;
            InitializeClient();
        }

        private HttpClient _apiClient;
        public HttpClient APIClient => _apiClient;

        private void InitializeClient()
        {
            string? api = _config.GetValue<string>("api");
            if (api != null)
            {
                _apiClient = new HttpClient();
                _apiClient.BaseAddress = new Uri(api);
                _apiClient.DefaultRequestHeaders.Accept.Clear();
                _apiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
        }

        public async Task<AuthenticatedUser> AuthenticateAsync(string username, string password)
        {
            var data = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            });

            try
            {
                using var response = await ApiRequest.SendAsync(_apiClient.PostAsync("/token", data));
                return await response.Content.ReadAsAsync<AuthenticatedUser>();
            }
            catch (HttpRequestException ex) when (ex.StatusCode.HasValue &&
                !ApiRequest.IsServiceFailure(ex) && ex.StatusCode != HttpStatusCode.TooManyRequests)
            {
                return null;
            }
        }

        public string GetLoggedInUserId()
        {
            if (_loggedInUser == null) { return null; }

            return _loggedInUser.Id;
        }

        public async Task GetLoggedInUserInfoAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/user/getcurrentuser");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using (HttpResponseMessage response = await ApiRequest.SendAsync(_apiClient.SendAsync(request)))
            {
                var result = await response.Content.ReadAsAsync<LoggedInUserModel>();
                _loggedInUser.Id = result.Id;
                _loggedInUser.Email = result.Email;
                _loggedInUser.FirstName = result.FirstName;
                _loggedInUser.LastName = result.LastName;
                _loggedInUser.CreatedDate = result.CreatedDate;
                _loggedInUser.Token = token;

                _apiClient.DefaultRequestHeaders.Authorization = request.Headers.Authorization;
                _apiClient.DefaultRequestHeaders.Accept.Clear();
                _apiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
        }

        public void LogOutUser()
        {
            _apiClient.DefaultRequestHeaders.Clear();
        }
    }
}
