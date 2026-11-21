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
    public class ContextController : ControllerBase
    {
        private readonly IContextData _contextData;

        public ContextController(IContextData contextData)
        {
            _contextData = contextData;
        }

        [HttpGet("{contextId}")]
        public ActionResult<ContextModel> GetContextById(int contextId)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var context = _contextData.GetContextById(contextId, userId);
            return context == null ? NoContent() : context;
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

        [HttpPut]
        public IActionResult Put(ContextModel updatedContext)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return _contextData.UpdateContextData(updatedContext, userId) ? Ok() : NotFound();
        }

        [HttpDelete]
        public IActionResult Delete(ContextModel contextToDelete)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _contextData.DeleteContext(contextToDelete.Id, userId);
            return Ok();
        }
    }
}
