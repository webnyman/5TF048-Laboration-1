using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using PracticeLogger.Models;
using PracticeLogger.Models.Api;

namespace PracticeLogger.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                DisplayName = dto.DisplayName ?? dto.Email
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(err.Code, err.Description);
                return BadRequest(ModelState);
            }

            // Du kan välja att default-assigna "User"-roll här om du vill
            // await _userManager.AddToRoleAsync(user, "User");

            var token = await GenerateJwtTokenAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            return new AuthResponseDto
            {
                UserId = user.Id.ToString(),
                DisplayName = user.DisplayName,
                Email = user.Email,
                Roles = roles
            };
        }

        // POST: api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Tillåt både email och username
            ApplicationUser? user =
                await _userManager.FindByEmailAsync(dto.EmailOrUserName)
                ?? await _userManager.FindByNameAsync(dto.EmailOrUserName);

            if (user == null)
                return Unauthorized(new { message = "Felaktiga inloggningsuppgifter." });

            var pwCheck = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
            if (!pwCheck.Succeeded)
                return Unauthorized(new { message = "Felaktiga inloggningsuppgifter." });

            var token = await GenerateJwtTokenAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            return new AuthResponseDto
            {
                Token = token.TokenString,
                ExpiresAt = token.ExpiresAt,
                UserId = user.Id.ToString(),
                DisplayName = user.DisplayName,
                Email = user.Email,
                Roles = roles
            };
        }

        private record TokenResult(string TokenString, DateTime ExpiresAt);

        private async Task<TokenResult> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var jwtSection = _config.GetSection("Jwt");
            var key = jwtSection["Key"]!;
            var issuer = jwtSection["Issuer"];
            var audience = jwtSection["Audience"];

            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
            };

            if (!string.IsNullOrWhiteSpace(user.DisplayName))
            {
                claims.Add(new Claim("DisplayName", user.DisplayName));
            }

            // Rollen / roller
            foreach (var r in roles)
                claims.Add(new Claim(ClaimTypes.Role, r));

            var keyBytes = Encoding.UTF8.GetBytes(key);
            var creds = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddHours(1);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return new TokenResult(tokenString, expires);
        }
    }
}
