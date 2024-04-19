using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;
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

        [HttpPut]
        public async Task<IActionResult> Update(ApplicationUserModel updatedUser)
        {
            if (ModelState.IsValid)
            {
                string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                UserModel currentUser = _userData.GetUserById(userId).First();
                IdentityUser? existingIdentityUser = await _userManager.FindByIdAsync(userId!);

                if (existingIdentityUser == null) { return BadRequest(); }
                else
                {
                    // update EFData - email + username (which is email)
                    existingIdentityUser.Email = updatedUser.Email;
                    existingIdentityUser.UserName = updatedUser.Email;
                    IdentityResult result = await _userManager.UpdateAsync(existingIdentityUser);

                    if (result.Succeeded)
                    {
                        // update TaskFocusData - email + names
                        UserModel updatedUserModel = new()
                        {
                            Id = existingIdentityUser.Id,
                            CreatedDate = currentUser.CreatedDate,
                            FirstName = updatedUser.FirstName,
                            LastName = updatedUser.LastName,
                            Email = updatedUser.Email
                        };

                        _userData.UpdateUser(updatedUserModel);

                        return Ok();
                    }
                }
            }

            return BadRequest();
        }

        [HttpPut]
        public async Task UpdatePassword(UserRegistrationModel updatedUserModel)
        {
            try
            {
                string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                IdentityUser? existingUser = await _userManager.FindByIdAsync(userId!);

                if (existingUser != null)
                {
                    if (!string.IsNullOrEmpty(existingUser.UserName))
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                        var passwordChangeResult = await _userManager.ResetPasswordAsync(existingUser, token, updatedUserModel.Password);

                        if (passwordChangeResult.Succeeded)
                        {
                            // TODO: send email, etc
                            //Console.WriteLine("password change: success");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
