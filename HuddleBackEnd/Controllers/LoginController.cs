using HuddleBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace HuddleBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly HuddleDbContext _context;

        public LoginController(HuddleDbContext context)
        {
            _context = context;
        }

        // POST: api/login
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] UserLogin login)
        {
            if (string.IsNullOrWhiteSpace(login.Username) || string.IsNullOrWhiteSpace(login.Password))
                return BadRequest("Username and password are required.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == login.Username);

            if (user == null)
                return Unauthorized("Invalid username or password.");

            var passwordHash = HashPassword(login.Password);
            if (user.PasswordHash != passwordHash)
                return Unauthorized("Invalid username or password.");

            return Ok(new
            {
                message = "Login successful!",
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.AvatarUrl,
                    user.CreatedAt
                }
            });
        }

        // Simple SHA256 password hashing (matches registration)
        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
    

    // DTO (Data Transfer Object)
    public class UserLogin
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
