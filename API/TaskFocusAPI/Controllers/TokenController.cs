using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TaskFocusAPI.Services;

namespace TaskFocusAPI.Controllers
{
    public class TokenController : Controller
    {
        private readonly IJwtKeyProvider _keys;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public TokenController(IJwtKeyProvider keys,
                               UserManager<IdentityUser> userManager,
                               SignInManager<IdentityUser> signInManager)
        {
            _keys = keys;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [Route("/token")]
        [HttpPost]
        public async Task<IActionResult> Create(string username, string password, string grant_type)
        {
            var user = await _userManager.FindByEmailAsync(username);
            if (user == null) { return BadRequest(); }

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
            if (result.IsNotAllowed && !user.EmailConfirmed)
            {
                await UnconfirmedPasswordCheck.CheckAsync(_userManager, user, password);
            }

            // Identity checks the password here before it evaluates a second factor.
            if (!result.Succeeded || await _userManager.GetTwoFactorEnabledAsync(user))
            {
                return BadRequest();
            }

            return new ObjectResult(await GenerateToken(user, username));
        }

        private async Task<TokenResponse> GenerateToken(IdentityUser user, string username)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Nbf, new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds().ToString()), // not before
                new Claim(JwtRegisteredClaimNames.Exp, new DateTimeOffset(DateTime.Now.AddDays(1)).ToUnixTimeSeconds().ToString()), // expiration
            };

            foreach (var role in await _userManager.GetRolesAsync(user))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                new JwtHeader(new SigningCredentials(_keys.SigningKey, SecurityAlgorithms.HmacSha256)),
                new JwtPayload(claims));

            return new TokenResponse(new JwtSecurityTokenHandler().WriteToken(token), username);
        }

        private sealed record TokenResponse(string AccessToken, string UserName);
    }
}
