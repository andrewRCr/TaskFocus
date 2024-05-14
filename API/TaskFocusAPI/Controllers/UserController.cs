using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;
using System.Security.Claims;
using TaskFocusAPI.Data;
using TaskFocusAPI.Library.Utilities;
using TaskFocusAPI.Library.DataAccess;
using TaskFocusAPI.Library.Models;
using TaskFocusAPI.Models;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Http.HttpResults;


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
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _config;

        public UserController(ApplicationDbContext context,
                              UserManager<IdentityUser> userManager,
                              IUserData userData,
                              IEmailSender emailSender,
                              IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _userData = userData;
            _emailSender = emailSender;
            _config = config;
        }

        [HttpGet]
        public async Task SendTestEmailToUser()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            UserModel currentUser = _userData.GetUserById(userId).First();

            UserModel testUser = new();
            testUser.FirstName = "TestFirst";
            testUser.LastName = "TestLast";
            testUser.Email = "andrew.creekmore@me.com";

            await _emailSender.SendEmailAsync(testUser, "test", "Hey, this is a test!");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<bool> CheckUserEmailConfirmed(UserModel userModel)
        {
            IdentityUser? user = await _userManager.FindByEmailAsync(userModel.Email);
            if (user != null) { return user.EmailConfirmed; }

            return false;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<bool> CheckUserExists(UserModel userModel)
        {
            IdentityUser? user = await _userManager.FindByEmailAsync(userModel.Email);
            if (user != null) { return true; }

            return false;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SendPasswordResetEmail(UserModel userModel)
        {
            IdentityUser? existingUser = await _userManager.FindByEmailAsync(userModel.Email);
            if (existingUser != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                string resetLink = $"{_config["AppUrl"]}/resetpw?email={existingUser.Email}&token={token}";

                UserModel currentUser = _userData.GetUserById(existingUser.Id).First();
                await _emailSender.SendPasswordResetLinkAsync(currentUser, resetLink);

                return StatusCode(StatusCodes.Status200OK,
                    new Response { Status = "Success", Message = $"Password reset request sent to {existingUser.Email}." });
            }

            return StatusCode(StatusCodes.Status400BadRequest,
                new Response { Status = "Error", Message = "Could not send password reset email, please try again." });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailModel confirmEmailModel)
        {
            IdentityUser? user = await _userManager.FindByEmailAsync(confirmEmailModel.Email);
            if (user != null)
            {
                string decodedToken = confirmEmailModel.Token.Replace(" ", "+");
                var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
                if (result.Succeeded)
                {
                    return StatusCode(StatusCodes.Status200OK,
                        new Response { Status = "Success", Message = "Email confirmed successfully." });
                }

                return StatusCode(StatusCodes.Status500InternalServerError,
                new Response { Status = "Error", Message = "Could not confirm email address - tokens do not match." });
            }

            return StatusCode(StatusCodes.Status500InternalServerError,
                new Response { Status = "Error", Message = "Could not confirm email address - user does not exist." });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            IdentityUser? existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                string decodedToken = model.Token.Replace(" ", "+");

                var result = await _userManager.ResetPasswordAsync(existingUser, decodedToken, model.Password);
                if (!result.Succeeded) 
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(error.Code, error.Description);
                    }
                    return Ok(ModelState);
                }
                else
                {
                    UserModel user = _userData.GetUserById(existingUser.Id).First();
                    await _emailSender.SendEmailAsync(user, "Password changed", "Your TaskFocus password has been updated.");
                    return Ok();
                }
            }

            return StatusCode(StatusCodes.Status400BadRequest,
            new Response { Status = "Error", Message = "Could not reset password, please try again." });
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
        public async Task<IActionResult> SendEmailConfirmationLink(UserModel userModel)
        {
            var existingUser = await _userManager.FindByEmailAsync(userModel.Email);

            if (existingUser != null) 
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(existingUser);
                string? confirmLink = $"{_config["AppUrl"]}/unconfirmedemail?email={userModel.Email}&token={token}";

                if (confirmLink != null)
                {
                    await _emailSender.SendConfirmationLinkAsync(userModel, confirmLink);

                    return Ok();
                }
            }

            return BadRequest();
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
                    IdentityUser newIdentityUser = new()
                    {
                        Email = user.Email,
                        UserName = user.Email,
                    };

                    IdentityResult result = await _userManager.CreateAsync(newIdentityUser, user.Password);
                    if (result.Succeeded)
                    {
                        existingUser = await _userManager.FindByEmailAsync(user.Email);
                        if (existingUser == null) { return BadRequest(); }

                        UserModel newUserModel = new UserModel() { Email = user.Email };
                        newUserModel.Id = existingUser.Id;
                        newUserModel.FirstName = user.FirstName;
                        newUserModel.LastName = user.LastName;

                        _userData.CreateUser(newUserModel);
                        await _userManager.AddToRoleAsync(existingUser, "User");

                        // send email confirmation link
                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(existingUser);
                        string? confirmLink = $"{_config["AppUrl"]}/unconfirmedemail?email={newUserModel.Email}&token={token}";

                        if (confirmLink != null)
                        {
                            await _emailSender.SendConfirmationLinkAsync(newUserModel, confirmLink);
                            return StatusCode(StatusCodes.Status200OK,
                               new Response { Status = "Success", Message = $"User successfully created and email confirmation link sent to {user.Email}" });
                        }
                    }
                }
            }

            return BadRequest();
        }

        [HttpPut]
        public async Task<IActionResult> UpdateName(ApplicationUserModel updatedUser)
        {
            if (ModelState.IsValid)
            {
                string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                UserModel currentUser = _userData.GetUserById(userId).First();
                IdentityUser? existingIdentityUser = await _userManager.FindByIdAsync(userId!);

                if (existingIdentityUser == null) { return BadRequest(); }
                else
                {
                    // update TaskFocusData - names
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

            return BadRequest();
        }

        [HttpPut]
        public async Task<IActionResult> RequestUpdateEmail(ApplicationUserModel updatedUser)
        {
            if (ModelState.IsValid)
            {
                string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                UserModel currentUser = _userData.GetUserById(userId).First();
                IdentityUser? existingIdentityUser = await _userManager.FindByIdAsync(userId!);

                if (existingIdentityUser == null) { return BadRequest(); }
                else
                {
                    string previousEmail = string.Empty;
                    bool emailChanged = existingIdentityUser.Email != updatedUser.Email;
                    if (emailChanged) { previousEmail = existingIdentityUser.Email!; }
                    else { return BadRequest(); }

                    // send email confirmation link
                    var token = await _userManager.GenerateChangeEmailTokenAsync(existingIdentityUser, updatedUser.Email!);
                    string? confirmLink = $"{_config["AppUrl"]}/unconfirmedupdatedemail?email={updatedUser.Email}&token={token}";

                    UserModel updatedUserModel = currentUser;
                    updatedUserModel.Email = updatedUser.Email;

                    if (confirmLink != null)
                    {
                        await _emailSender.SendConfirmationLinkAsync(updatedUserModel, confirmLink);
                        return Ok();
                    }
                }
            }

            return BadRequest();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmUpdatedEmail(ConfirmUpdatedEmailModel confirmUpdatedEmailModel)
        {
            IdentityUser? identityUser = await _userManager.FindByEmailAsync(confirmUpdatedEmailModel.OldEmail);
            if (identityUser != null)
            {
                string decodedToken = confirmUpdatedEmailModel.Token.Replace(" ", "+");
                IdentityResult result = await _userManager.ChangeEmailAsync(identityUser, confirmUpdatedEmailModel.NewEmail, decodedToken);

                if (result.Succeeded)
                {
                    // update EFData - username (which is email) 
                    identityUser.UserName = confirmUpdatedEmailModel.NewEmail;
                    IdentityResult nextResult = await _userManager.UpdateAsync(identityUser);

                    if (nextResult.Succeeded)
                    {
                        // update TaskFocusData - email
                        UserModel userModel = _userData.GetUserById(identityUser.Id).First();
                        userModel.Email = confirmUpdatedEmailModel.NewEmail;
                        _userData.UpdateUser(userModel);
                    }

                    return StatusCode(StatusCodes.Status200OK,
                        new Response { Status = "Success", Message = "Update email confirmed successfully." });
                }

                return StatusCode(StatusCodes.Status500InternalServerError,
                new Response { Status = "Error", Message = "Could not confirm updated email address - tokens do not match." });
            }

            return StatusCode(StatusCodes.Status500InternalServerError,
                new Response { Status = "Error", Message = "Could not confirm updated email address - user does not exist." });
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
                            UserModel user = _userData.GetUserById(existingUser.Id).First();
                            await _emailSender.SendEmailAsync(user, "Password changed", "Your TaskFocus password has been updated. ");
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
