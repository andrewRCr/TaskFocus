using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using TaskFocusUI.Library.API;

namespace TaskFocusWeb.Authentication
{
    public class AuthStateProvider : AuthenticationStateProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private readonly IConfiguration _config;
        private readonly IAPIHelper _apiHelper;
        private readonly AppState _appState;
        private readonly AuthenticationState _anonymous;

        public AuthStateProvider(HttpClient httpClient,
                                 ILocalStorageService localStorage,
                                 IConfiguration config,
                                 IAPIHelper apiHelper,
                                 AppState appState)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
            _config = config;
            _apiHelper = apiHelper;
            _appState = appState;
            _anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new ClaimsIdentity())));
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            string? authTokenStorageKey = _config["authTokenStorageKey"];
            if (authTokenStorageKey is null) { return _anonymous; }

            var token = await _localStorage.GetItemAsync<string>(authTokenStorageKey);
            if (string.IsNullOrWhiteSpace(token)) { return _anonymous; }

            try
            {
                var authState = await LoadAuthenticatedStateAsync(token);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
                return authState;
            }
            catch (Exception ex) when (ex is FormatException or JsonException
                || ex is HttpRequestException requestError && CredentialsRejected(requestError))
            {
                await ClearSessionAsync();
                ShowSessionError("Session expired; please log in again.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                ShowSessionError("Could not load your profile. Please try logging in again.");
            }

            return _anonymous;
        }

        public async Task NotifyUserAuthenticationAsync(string token)
        {
            var authState = await LoadAuthenticatedStateAsync(token);
            try
            {
                if (_config["authTokenStorageKey"] is { } key)
                {
                    await _localStorage.SetItemAsync(key, token);
                }
            }
            catch
            {
                _apiHelper.LogOutUser();
                throw;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }

        private async Task<AuthenticationState> LoadAuthenticatedStateAsync(string token)
        {
            var identity = new ClaimsIdentity(JwtParser.ParseClaimsFromJwt(token), "jwtAuthType");
            await _apiHelper.GetLoggedInUserInfoAsync(token);
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        internal static bool CredentialsRejected(HttpRequestException ex) =>
            ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;

        private void ShowSessionError(string message)
        {
            _appState.AlertSeverity = MudBlazor.Severity.Warning;
            _appState.AlertMessage = message;
        }

        public async Task NotifyUserLogoutAsync()
        {
            await ClearSessionAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
        }

        private async Task ClearSessionAsync()
        {
            string? authTokenStorageKey = _config["authTokenStorageKey"];
            if (authTokenStorageKey != null)
            {
                await _localStorage.RemoveItemAsync(authTokenStorageKey);
            }

            _apiHelper.LogOutUser();
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}
