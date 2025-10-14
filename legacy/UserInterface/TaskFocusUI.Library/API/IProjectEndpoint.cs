using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.API
{
    public interface IProjectEndpoint
    {
        Task<ProjectModel> GetProjectById(int projectId);
        Task<List<ProjectModel>> GetAllProjectsForUser();
        Task<ProjectModel> AddProject(ProjectModel newProject, string userId);
        Task UpdateProject(ProjectModel projectToUpdate);
        Task DeleteProject(ProjectModel projectToDelete);
    }
}