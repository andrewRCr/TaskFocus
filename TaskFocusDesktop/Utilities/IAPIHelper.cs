using System.Threading.Tasks;
using TaskFocusDesktop.Models;

namespace TaskFocusDesktop.Utilities
{
    public interface IAPIHelper
    {
        Task<AuthenticatedUser> Authenticate(string username, string password);
    }
}