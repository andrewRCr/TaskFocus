using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    }
}
