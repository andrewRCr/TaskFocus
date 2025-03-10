using System.Collections.Generic;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Library.DataAccess
{
    public interface IProjectData
    {
        ProjectModel GetProjectById(int projectId);
        List<ProjectModel> GetAllProjectsForUser(string userId);
        ProjectModel AddProject(ProjectModel newProject, string userId);
        void DeleteProject(ProjectModel projectToDelete);
        void UpdateProjectData(ProjectModel frontEndProject);
    }
}