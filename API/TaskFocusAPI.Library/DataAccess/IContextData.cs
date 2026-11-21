using System.Collections.Generic;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Library.DataAccess
{
    public interface IContextData
    {
        ContextModel GetContextById(int contextId, string userId);
        List<ContextModel> GetAllContextsForUser(string userId);
        ContextModel AddContext(ContextModel newContext, string userId);
        bool DeleteContext(int? contextId, string userId);
        bool UpdateContextData(ContextModel frontEndContext, string userId);
    }
}