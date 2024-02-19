using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFocusDesktop.Library.Models;

namespace TaskFocusDesktop.Library.API
{
    public interface IProjectEndpoint
    {
        Task<List<ProjectModel>> GetAllProjectsForUser();
        Task AddProject(ProjectModel newProject, string userId);
        Task DeleteProject(ProjectModel projectToDelete);
        Task UpdateProject(ProjectModel projectToUpdate);
    }
}