using Microsoft.AspNetCore.Components.Authorization;
using System.Net;
using System.Text.Json;
using TaskFocusUI.Library.API;
using TaskFocusWeb.Models;

namespace TaskFocusWeb.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly IConfiguration _config;

        public AuthenticationService(HttpClient httpClient,
                                     AuthenticationStateProvider authStateProvider,
                                     IConfiguration config)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
            _config = config;
        }

        public async Task<AuthenticatedUserModel?> LoginAsync(AuthenticationUserModel userToAuthenticate)
        {
            using var data = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", userToAuthenticate.Email),
                new KeyValuePair<string, string>("password", userToAuthenticate.Password)
            });

            string apiAuthUri = _config["api"] + _config["tokenEndpoint"];
            HttpResponseMessage response;
            try
            {
                response = await ApiRequest.SendAsync(_httpClient.PostAsync(apiAuthUri, data));
            }
            catch (HttpRequestException ex) when (ex.StatusCode is
                HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return null;
            }

            using var authResult = response;
            var authContent = await authResult.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AuthenticatedUserModel>(
                authContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result != null)
            {
                if (string.IsNullOrWhiteSpace(result.AccessToken))
                {
                    throw new JsonException("Missing access token.");
                }
                await ((AuthStateProvider)_authStateProvider).NotifyUserAuthenticationAsync(result.AccessToken);
            }

            return result;
        }

        public async Task LogoutAsync()
        {
            await ((AuthStateProvider)_authStateProvider).NotifyUserLogoutAsync();
        }
    }
}
