using System.Net.Http;
using System.Threading.Tasks;
using TaskFocusUI.Library.API;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.API
{
    public interface IAPIHelper
    {
        HttpClient APIClient { get; }
        Task<AuthenticatedUser> AuthenticateAsync(string username, string password);
        string GetLoggedInUserId();
        Task GetLoggedInUserInfoAsync(string token);
        void LogOutUser();
    }
}