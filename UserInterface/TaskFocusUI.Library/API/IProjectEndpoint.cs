using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFocusUI.Library.Models;

namespace TaskFocusUI.Library.API
{
    public interface IProjectEndpoint
    {
        Task<List<ProjectModel>> GetAllProjectsForUser();
        Task AddProject(ProjectModel newProject, string userId);
        Task DeleteProject(ProjectModel projectToDelete);
        Task UpdateProject(ProjectModel projectToUpdate);
    }
}