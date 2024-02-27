using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using TaskFocusUI.Library.API;

namespace TaskFocusWeb.Authentication
{
    public class AuthStateProvider : AuthenticationStateProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private readonly IConfiguration _config;
        private readonly IAPIHelper _apiHelper;
        private readonly AuthenticationState _anonymous;

        public AuthStateProvider(HttpClient httpClient,
                                 ILocalStorageService localStorage,
                                 IConfiguration config,
                                 IAPIHelper apiHelper)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
            _config = config;
            _apiHelper = apiHelper;
            _anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new ClaimsIdentity())));
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            string? authTokenStorageKey = _config["authTokenStorageKey"];
            if (authTokenStorageKey != null)
            {
                var token = await _localStorage.GetItemAsync<string>(authTokenStorageKey);
                if (string.IsNullOrWhiteSpace(token)) { return _anonymous; }

                bool isAuthenticated = await NotifyUserAuthenticationAsync(token);
                if (!isAuthenticated) { return _anonymous; }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);

                return new AuthenticationState(
                    new ClaimsPrincipal(
                        new ClaimsIdentity(JwtParser.ParseClaimsFromJwt(token),
                        "jwtAuthType")));
            }

            return _anonymous;
        }

        public async Task<bool> NotifyUserAuthenticationAsync(string token)
        {
            bool isAuthenticated;
            Task<AuthenticationState> authState;

            try
            {
                await _apiHelper.GetLoggedInUserInfoAsync(token);

                var authenticatedUser = new ClaimsPrincipal(
                    new ClaimsIdentity(JwtParser.ParseClaimsFromJwt(token),
                    "jwtAuthType"));
                authState = Task.FromResult(new AuthenticationState(authenticatedUser));

                NotifyAuthenticationStateChanged(authState);
                isAuthenticated = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await NotifyUserLogoutAsync();
                isAuthenticated = false;
            }   

            return isAuthenticated;
        }

        public async Task NotifyUserLogoutAsync()
        {
            string? authTokenStorageKey = _config["authTokenStorageKey"];
            if (authTokenStorageKey != null)
            {
                await _localStorage.RemoveItemAsync(authTokenStorageKey);
            }

            var authState = Task.FromResult(_anonymous);
            _apiHelper.LogOutUser();
            _httpClient.DefaultRequestHeaders.Authorization = null;

            NotifyAuthenticationStateChanged(authState);
        }
    }
}
