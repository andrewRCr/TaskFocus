using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.API
{
    public interface IUserEndpoint
    {
        Task<List<UserModel>> GetAllUsers();
    }
}