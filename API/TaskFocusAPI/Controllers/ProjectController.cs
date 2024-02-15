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
        private readonly IConfiguration _config;

        public ProjectController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public List<ProjectModel> GetAllProjectsForUser()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ProjectData data = new ProjectData(_config);

            return data.GetAllProjectsForUser(userId);
        }
    }
}
