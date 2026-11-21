using System.Collections.Generic;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Library.DataAccess
{
    public interface IProjectData
    {
        ProjectModel GetProjectById(int projectId, string userId);
        List<ProjectModel> GetAllProjectsForUser(string userId);
        ProjectModel AddProject(ProjectModel newProject, string userId);
        bool DeleteProject(int? projectId, string userId);
        ResourceUpdateResult UpdateProjectData(ProjectModel frontEndProject, string userId);
    }
}