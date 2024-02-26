using TaskFocusWeb.Models;

namespace TaskFocusWeb.Authentication
{
    public interface IAuthenticationService
    {
        Task<AuthenticatedUserModel?> Login(AuthenticationUserModel userToAuthenticate);
        Task Logout();
    }
}