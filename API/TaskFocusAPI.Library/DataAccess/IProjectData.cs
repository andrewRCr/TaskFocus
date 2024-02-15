using System.Collections.Generic;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Library.DataAccess
{
    public interface IProjectData
    {
        List<ProjectModel> GetAllProjectsForUser(string userId);
    }
}