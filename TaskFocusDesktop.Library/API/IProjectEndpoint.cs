using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFocusDesktop.Library.Models;

namespace TaskFocusDesktop.Library.API
{
    public interface IProjectEndpoint
    {
        Task<List<ProjectModel>> GetAllForUser();
    }
}