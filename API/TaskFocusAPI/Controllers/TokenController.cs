using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskFocusAPI.Data;

namespace TaskFocusAPI.Controllers
{
    public class TokenController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public TokenController(IConfiguration config,
                               ApplicationDbContext context,
                               UserManager<IdentityUser> userManager)
        {
            _config = config;
            _context = context;
            _userManager = userManager;
        }

        [Route("/token")]
        [HttpPost]
        public async Task<IActionResult> Create(string username, string password, string grant_type)
        {
            if (await IsValidUsernameAndPassword(username, password))
            {
                return new ObjectResult(await GenerateToken(username));
            }
            else { return BadRequest(); }
        }

        private async Task<bool> IsValidUsernameAndPassword(string username, string password)
        {
            var user = await _userManager.FindByEmailAsync(username);

            if (user == null) { return false; }
            return await _userManager.CheckPasswordAsync(user, password);
        }

        private async Task<dynamic> GenerateToken(string username)
        {
            var user = await _userManager.FindByEmailAsync(username);

            if (user != null) 
            {
                var roles = from ur in _context.UserRoles
                            join role in _context.Roles on ur.RoleId equals role.Id
                            where ur.UserId == user.Id
                            select new { ur.UserId, ur.RoleId, role.Name };

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(JwtRegisteredClaimNames.Nbf, new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds().ToString()), // not before
                    new Claim(JwtRegisteredClaimNames.Exp, new DateTimeOffset(DateTime.Now.AddDays(1)).ToUnixTimeSeconds().ToString()), // expiration
                };

                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.Name));
                }

                string? securityKey = _config.GetValue<string>("Secrets:SecurityKey");

                if (securityKey != null)
                {
                    var token = new JwtSecurityToken(
                        new JwtHeader(
                            new SigningCredentials(
                                            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey)),
                                                SecurityAlgorithms.HmacSha256)),
                       new JwtPayload(claims)
                    );

                    var output = new
                    {
                        AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                        UserName = username,
                    };

                    return output;
                }
                else
                {
                    throw new Exception("SecurityKey was a null value!");
                }
            }

            return Task.CompletedTask;
        }
    }
}
