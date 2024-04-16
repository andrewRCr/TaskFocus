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
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserData _userData;

        public UserController(ApplicationDbContext context,
                              UserManager<IdentityUser> userManager,
                              IUserData userData)
        {
            _context = context;
            _userManager = userManager;
            _userData = userData;
        }

        [HttpGet]
        public UserModel GetCurrentUser()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return _userData.GetUserById(userId).First();
        }

        [HttpGet]
        public UserSettingsModel GetCurrentUserSettings()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return _userData.GetUserSettingsById(userId);
        }

        [HttpPut]
        [Route("settings")]
        public void Put(UserSettingsModel updatedSettings)
        {
            _userData.UpdateSettingsData(updatedSettings);
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

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(UserRegistrationModel user)
        {
            if (ModelState.IsValid) // TODO: implement validation!
            {
                var existingUser = await _userManager.FindByEmailAsync(user.Email);
                if (existingUser == null)
                {
                    IdentityUser newUser = new()
                    {
                        Email = user.Email,
                        EmailConfirmed = true, // TODO: need to implement email confirm link sending!
                        UserName = user.Email,
                    };

                    IdentityResult result = await _userManager.CreateAsync(newUser, user.Password);

                    if (result.Succeeded)
                    {
                        existingUser = await _userManager.FindByEmailAsync(user.Email);
                        if (existingUser == null)
                        {
                            return BadRequest();
                        }

                        UserModel newUserModel = new()
                        {
                            Id = existingUser.Id,
                            FirstName = user.FirstName, 
                            LastName = user.LastName, 
                            Email = user.Email
                        };

                        _userData.CreateUser(newUserModel);
                        await _userManager.AddToRoleAsync(existingUser, "User");

                        return Ok();
                    }
                }
            }

            return BadRequest();
        }
    }
}
