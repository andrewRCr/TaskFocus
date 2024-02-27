using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Text.Json;
using TaskFocusWeb.Models;

namespace TaskFocusWeb.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly ILocalStorageService _localStorage;
        private readonly IConfiguration _config;
        private readonly string? authTokenStorageKey;

        public AuthenticationService(HttpClient httpClient,
                                     AuthenticationStateProvider authStateProvider,
                                     ILocalStorageService localStorage,
                                     IConfiguration config)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
            _localStorage = localStorage;
            _config = config;
            authTokenStorageKey = _config["authTokenStorageKey"];
        }

        public async Task<AuthenticatedUserModel?> LoginAsync(AuthenticationUserModel userToAuthenticate)
        {
            var data = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", userToAuthenticate.Email),
                new KeyValuePair<string, string>("password", userToAuthenticate.Password)
            });

            string apiAuthUri = _config["api"] + _config["tokenEndpoint"];
            var authResult = await _httpClient.PostAsync(apiAuthUri, data);
            var authContent = await authResult.Content.ReadAsStringAsync();

            if (!authResult.IsSuccessStatusCode)
            {
                return null;
            }

            var result = JsonSerializer.Deserialize<AuthenticatedUserModel>(
                authContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result != null && authTokenStorageKey != null)
            {
                await _localStorage.SetItemAsync(authTokenStorageKey, result.AccessToken);
                await ((AuthStateProvider)_authStateProvider).NotifyUserAuthenticationAsync(result.AccessToken);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", result.AccessToken);
            }

            return result;
        }

        public async Task LogoutAsync()
        {
            await ((AuthStateProvider)_authStateProvider).NotifyUserLogoutAsync();
        }
    }
}
