using TaskFocusWeb.Models;

namespace TaskFocusWeb.Authentication
{
    public interface IAuthenticationService
    {
        Task<AuthenticatedUserModel?> LoginAsync(AuthenticationUserModel userToAuthenticate);
        Task LogoutAsync();
    }
}