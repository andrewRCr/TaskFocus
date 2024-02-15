using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskFocusAPI.Data;
using TaskFocusAPI.Library.DataAccess;
using TaskFocusAPI.Library.Models;
using TaskFocusAPI.Models;

namespace TaskFocusAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UserController(IConfiguration config, ApplicationDbContext context, UserManager<IdentityUser> userManager) 
        {
            _config = config;
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public UserModel GetById()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            UserData data = new UserData(_config);

            return data.GetUserById(userId).First();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [Route("Admin/GetAllUsers")]
        public List<ApplicationUserModel> GetAllUsers()
        {
            List<ApplicationUserModel> applicationUsers = new List<ApplicationUserModel>();

            var users = _context.Users.ToList();
            var userRoles = from user in _context.UserRoles
                            join role in _context.Roles on user.RoleId equals role.Id
                            select new { user.UserId, user.RoleId, role.Name };

            foreach (var user in users)
            {
                ApplicationUserModel applicationUserModel = new ApplicationUserModel
                {
                    Id = user.Id,
                    Email = user.Email
                };

                applicationUserModel.Roles = userRoles.Where(x => x.UserId == user.Id)
                    .ToDictionary(key => key.RoleId, val => val.Name);

                applicationUsers.Add(applicationUserModel);
            }

            return applicationUsers;
        }
    }
}
