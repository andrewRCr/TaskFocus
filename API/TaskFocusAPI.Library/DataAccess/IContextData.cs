using System.Collections.Generic;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Library.DataAccess
{
    public interface IContextData
    {
        ContextModel GetContextById(int contextId);
        List<ContextModel> GetAllContextsForUser(string userId);
        ContextModel AddContext(ContextModel newContext, string userId);
        void DeleteContext(ContextModel contextToDelete);
        void UpdateContextData(ContextModel frontEndContext);
    }
}