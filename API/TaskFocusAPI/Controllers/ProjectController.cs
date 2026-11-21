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
        public ActionResult<ProjectModel> GetProjectById(int projectId)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var project = _projectData.GetProjectById(projectId, userId);
            return project == null ? NoContent() : project;
        }

        [HttpGet]
        public List<ProjectModel> GetAllProjectsForUser()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return _projectData.GetAllProjectsForUser(userId);
        }

        [HttpPost]
        public ActionResult<ProjectModel> Post(ProjectModel newProject)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var insertedProject = _projectData.AddProject(newProject, userId);
            return insertedProject == null ? BadRequest() : insertedProject;
        }

        [HttpPut]
        public IActionResult Put(ProjectModel updatedProject)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return _projectData.UpdateProjectData(updatedProject, userId) switch
            {
                ResourceUpdateResult.Updated => Ok(),
                ResourceUpdateResult.InvalidParent => BadRequest(),
                _ => NotFound()
            };
        }

        [HttpDelete]
        public IActionResult Delete(ProjectModel projectToDelete)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _projectData.DeleteProject(projectToDelete.Id, userId);
            return Ok();
        }
    }
}
