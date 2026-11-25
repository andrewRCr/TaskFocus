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
    public class ContextController : ControllerBase
    {
        private const int DataTruncation = 8152;
        private const int VerboseDataTruncation = 2628;
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
        public IActionResult Post(ContextModel newContext) => CreateContext(newContext, includeResult: false);

        [HttpPost]
        public IActionResult PostWithResult(ContextModel newContext) => CreateContext(newContext, includeResult: true);

        private IActionResult CreateContext(ContextModel newContext, bool includeResult)
        {
            if (newContext.ContextName == null) { return BadRequest(); }

            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var insertedContext = _contextData.AddContext(newContext, userId);
                return includeResult ? Ok(insertedContext) : new EmptyResult();
            }
            catch (SqlException ex) when (ex.Number is DataTruncation or VerboseDataTruncation)
            {
                return BadRequest();
            }
        }

        [HttpPut]
        public IActionResult Put(ContextModel updatedContext)
        {
            if (updatedContext.Id is null or <= 0 || updatedContext.ContextName == null)
            {
                return BadRequest();
            }

            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                return _contextData.UpdateContextData(updatedContext, userId) ? Ok() : NotFound();
            }
            catch (SqlException ex) when (ex.Number is DataTruncation or VerboseDataTruncation)
            {
                return BadRequest();
            }
        }

        [HttpDelete]
        public IActionResult Delete(ContextModel contextToDelete)
        {
            if (contextToDelete.Id is null or <= 0) { return BadRequest(); }

            const int foreignKeyViolation = 547;
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                _contextData.DeleteContext(contextToDelete.Id, userId);
                return Ok();
            }
            catch (SqlException ex) when (ex.Number == foreignKeyViolation)
            {
                return Conflict();
            }
        }
    }
}
