using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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
        private const int MaxProjectNameLength = 128;
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
            // SQL Server allows excess trailing spaces on insert.
            if (newProject.ProjectName == null || newProject.ProjectName.TrimEnd(' ').Length > MaxProjectNameLength)
            {
                return BadRequest();
            }

            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var insertedProject = _projectData.AddProject(newProject, userId);
            return insertedProject == null ? BadRequest() : insertedProject;
        }

        [HttpPut]
        public IActionResult Put(ProjectModel updatedProject)
        {
            if (updatedProject.Id is null or <= 0 || updatedProject.ProjectName == null ||
                updatedProject.ProjectName.Trim().Length > MaxProjectNameLength)
            {
                return BadRequest();
            }

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
            if (projectToDelete.Id is null or <= 0) { return BadRequest(); }

            const int foreignKeyViolation = 547;
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                _projectData.DeleteProject(projectToDelete.Id, userId);
                return Ok();
            }
            catch (SqlException ex) when (ex.Number == foreignKeyViolation)
            {
                return Conflict();
            }
        }
    }
}
