using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.API
{
    public interface IContextEndpoint
    {
        Task<ContextModel> GetContextById(int contextId);
        Task<List<ContextModel>> GetAllContextsForUser();
        Task<ContextModel> AddContext(ContextModel newContext, string userId);
        Task UpdateContext(ContextModel contextToUpdate);
        Task DeleteContext(ContextModel contextToDelete);
    }
}