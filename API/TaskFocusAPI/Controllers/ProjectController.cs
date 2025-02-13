using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskFocusAPI.Library.DataAccess;
using TaskFocusAPI.Library.Models;

namespace TaskFocusAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectData _projectData;

        public ProjectController(IProjectData projectData)
        {
            _projectData = projectData;
        }

        [HttpGet("{projectId}")]
        public ProjectModel GetProjectById(int projectId)
        {
            return _projectData.GetProjectById(projectId);
        }

        [HttpGet]
        public List<ProjectModel> GetAllProjectsForUser()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return _projectData.GetAllProjectsForUser(userId);
        }

        [HttpPost]
        public void Post(ProjectModel newProject)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _projectData.AddProject(newProject, userId);
        }

        [HttpPut]
        public void Put(ProjectModel updatedProject)
        {
            _projectData.UpdateProjectData(updatedProject);
        }

        [HttpDelete]
        public void Delete(ProjectModel projectToDelete)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _projectData.DeleteProject(projectToDelete);
        }
    }
}
