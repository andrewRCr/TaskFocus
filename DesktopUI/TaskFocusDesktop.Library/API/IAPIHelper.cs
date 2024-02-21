using System.Net.Http;
using System.Threading.Tasks;
using TaskFocusDesktop.Library.API;
using TaskFocusDesktop.Library.Models;

namespace TaskFocusDesktop.Library.API
{
    public interface IAPIHelper
    {
        HttpClient APIClient { get; }
        Task<AuthenticatedUser> Authenticate(string username, string password);
        string GetLoggedInUserId();
        Task GetLoggedInUserInfo(string token);
        void LogOutUser();
    }
}