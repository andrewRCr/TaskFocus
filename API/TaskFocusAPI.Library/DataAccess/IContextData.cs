using System.Collections.Generic;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Library.DataAccess
{
    public interface IContextData
    {
        List<ContextModel> GetAllContextsForUser(string userId);
    }
}