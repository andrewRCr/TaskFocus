using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFocusDesktop.Library.Models;

namespace TaskFocusDesktop.Library.API
{
    public interface IContextEndpoint
    {
        Task<List<ContextModel>> GetAllContextsForUser();
        Task AddContext(ContextModel newContext, string userId);
        Task DeleteContext(ContextModel contextToDelete);
        Task UpdateContext(ContextModel contextToUpdate);
    }
}