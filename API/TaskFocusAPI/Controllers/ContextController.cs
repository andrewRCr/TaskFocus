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
    public class ContextController : ControllerBase
    {
        private readonly IContextData _contextData;

        public ContextController(IContextData contextData)
        {
            _contextData = contextData;
        }

        [HttpGet("{contextId}")]
        public ContextModel GetContextById(int contextId)
        {
            return _contextData.GetContextById(contextId);
        }

        [HttpGet]
        public List<ContextModel> GetAllContextsForUser()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return _contextData.GetAllContextsForUser(userId);
        }

        [HttpPost]
        public void Post(ContextModel newContext)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _contextData.AddContext(newContext, userId);
        }
    }
}
