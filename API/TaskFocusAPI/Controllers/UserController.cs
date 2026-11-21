using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using TaskFocusAPI.Data;
using TaskFocusAPI.Library.DataAccess;
using TaskFocusAPI.Library.Models;
using TaskFocusAPI.Library.Utilities;
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
        private readonly IEmailSender _emailSender;
        private readonly string _appUrl;

        public UserController(ApplicationDbContext context,
                              UserManager<IdentityUser> userManager,
                              IUserData userData,
                              IEmailSender emailSender,
                              [FromKeyedServices("AppUrl")] string appUrl)
        {
            _context = context;
            _userManager = userManager;
            _userData = userData;
            _emailSender = emailSender;
            _appUrl = appUrl;
        }

        [HttpGet]
        public async Task SendTestEmailToUser()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            UserModel currentUser = _userData.GetUserById(userId).First();

            await _emailSender.SendEmailAsync(currentUser, "test", "Hey, this is a test!");
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
        public async Task<bool> CheckPasswordValid(CheckPasswordModel checkPasswordModel)
        {
            IdentityUser? identityUser = await _userManager.FindByEmailAsync(checkPasswordModel.Email);
            if (identityUser != null) 
            {
                return await _userManager.CheckPasswordAsync(identityUser, checkPasswordModel.Password);
            }

            return false;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SendPasswordResetEmail(UserModel userModel)
        {
            IdentityUser? existingUser = await _userManager.FindByEmailAsync(userModel.Email);
            if (existingUser?.Email is string email)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                string resetLink = BuildAccountLink("/resetpw", email, token);

                UserModel recipient = GetRecoveryRecipient(existingUser.Id, email);
                await _emailSender.SendPasswordResetLinkAsync(recipient, resetLink);

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
                    return StatusCode(StatusCodes.Status200OK,
                        new Response { Status = "Success", Message = "Password changed successfully." });
                }
            }

            return StatusCode(StatusCodes.Status400BadRequest,
            new Response { Status = "Error", Message = "Could not reset password, please try again." });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SendPasswordChangeSuccessEmail(UserModel userModel)
        {
            IdentityUser? identityUser = await _userManager.FindByEmailAsync(userModel.Email);
            if (identityUser?.Email is string email)
            {
                UserModel recipient = GetRecoveryRecipient(identityUser.Id, email);
                await _emailSender.SendEmailAsync(recipient, "Password changed", "Hello,<br><br>Your TaskFocus password has been successfully updated.");
                return StatusCode(StatusCodes.Status200OK,
                new Response { Status = "Success", Message = $"Password changed successfully email sent to {identityUser.Email}." });
            }
            else
            {
                return StatusCode(StatusCodes.Status400BadRequest,
                new Response { Status = "Error", Message = "Could not send password changed success email, please try again." });
            }
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
        public IActionResult Put(UserSettingsModel updatedSettings)
        {
            if (updatedSettings.CleanUpDelayDays is < 1 or > 90 ||
                updatedSettings.DeleteDelayDays is < 1 or > 90)
            {
                return BadRequest();
            }

            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return _userData.UpdateSettingsData(updatedSettings, userId) ? Ok() : NotFound();
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

            if (existingUser?.Email is string email)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(existingUser);
                string confirmLink = BuildAccountLink("/unconfirmedemail", email, token);
                userModel.Email = email;
                await _emailSender.SendConfirmationLinkAsync(userModel, confirmLink);
                return Ok();
            }

            return BadRequest();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(UserRegistrationModel user)
        {
            if (ModelState.IsValid)
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
                        string confirmLink = BuildAccountLink("/unconfirmedemail", newUserModel.Email, token);
                        await _emailSender.SendConfirmationLinkAsync(newUserModel, confirmLink);
                        return StatusCode(StatusCodes.Status200OK,
                           new Response { Status = "Success", Message = $"User successfully created and email confirmation link sent to {user.Email}" });
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
                IdentityUser? existingIdentityUser = await _userManager.FindByIdAsync(userId!);

                if (existingIdentityUser == null) { return BadRequest(); }
                else
                {
                    // update TaskFocusData - names
                    UserModel updatedUserModel = new()
                    {
                        Id = existingIdentityUser.Id,
                        FirstName = updatedUser.FirstName,
                        LastName = updatedUser.LastName,
                        ServerLastUpdated = updatedUser.ServerLastUpdated,
                        ClientLastUpdated = updatedUser.ClientLastUpdated
                    };

                    return _userData.UpdateUserName(updatedUserModel)
                        ? Ok()
                        : StatusCode(StatusCodes.Status500InternalServerError);
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
                    string confirmLink = BuildAccountLink("/unconfirmedupdatedemail", updatedUser.Email, token);

                    UserModel updatedUserModel = currentUser;
                    updatedUserModel.Email = updatedUser.Email;

                    await _emailSender.SendConfirmationLinkAsync(updatedUserModel, confirmLink);
                    return Ok();
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
                        await _userManager.ResetPasswordAsync(existingUser, token, updatedUserModel.Password);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private UserModel GetRecoveryRecipient(string userId, string email)
        {
            UserModel recipient = _userData.GetUserById(userId).FirstOrDefault() ?? new UserModel();
            recipient.Email = email;
            return recipient;
        }

        private string BuildAccountLink(string path, string? email, string token)
        {
            return QueryHelpers.AddQueryString($"{_appUrl}{path}", new Dictionary<string, string?>
            {
                ["email"] = email ?? string.Empty,
                ["token"] = token
            });
        }
    }
}
