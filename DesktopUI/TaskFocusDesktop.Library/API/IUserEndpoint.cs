using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFocusDesktop.Library.Models;

namespace TaskFocusDesktop.Library.API
{
    public interface IUserEndpoint
    {
        Task<List<UserModel>> GetAllUsers();
    }
}