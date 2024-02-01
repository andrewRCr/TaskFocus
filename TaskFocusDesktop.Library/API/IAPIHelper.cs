using System.Net.Http;
using System.Threading.Tasks;
using TaskFocusDesktop.Library.API;

namespace TaskFocusDesktop.Library.API
{
    public interface IAPIHelper
    {
        HttpClient APIClient { get; }
        Task<AuthenticatedUser> Authenticate(string username, string password);
        Task GetLoggedInUserInfo(string token);
    }
}