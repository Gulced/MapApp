using MapApp.Domain.Entities;
using MapApp.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MapApp.API.Controllers
{
    public class AuthRequestDto
    {
        public string? Username { get; set; }  // Dışarıdan gelen alan olarak kalabilir
        public string? Password { get; set; }
        public string? Email { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _dbContext;

        public AuthController(IConfiguration config, AppDbContext dbContext)
        {
            _config = config;
            _dbContext = dbContext;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Kullanıcı adı, e-posta ve şifre zorunludur.");
            }

            if (await _dbContext.Users.AnyAsync(u => u.UserName == request.Username))
                return BadRequest("Kullanıcı adı zaten mevcut.");

            if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest("E-posta zaten kayıtlı.");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var user = new AppUser
            {
                UserName = request.Username,   // Burada UserName olmalı
                PasswordHash = passwordHash,
                Role = "User",
                Email = request.Email
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Kayıt sonrası token üret ve dön
            var token = CreateJwt(user);
            return Ok(new { token });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Kullanıcı adı ve şifre zorunludur.");
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == request.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized("Geçersiz kullanıcı adı veya şifre.");

            var token = CreateJwt(user);
            return Ok(new { token });
        }

        private string CreateJwt(AppUser user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),  // Burada UserName olmalı
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
