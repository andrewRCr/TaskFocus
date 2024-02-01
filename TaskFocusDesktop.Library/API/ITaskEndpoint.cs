using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFocusDesktop.Library.Models;

namespace TaskFocusDesktop.Library.API
{
    public interface ITaskEndpoint
    {
        Task<List<TaskModel>> GetAllForUser();
    }
}