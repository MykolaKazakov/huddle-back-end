using HuddleBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Text;

namespace HuddleBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegistrationController : ControllerBase
    {
        private readonly HuddleDbContext _context;

        public RegistrationController(HuddleDbContext context)
        {
            _context = context;
        }

        // POST: api/registration
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] UserRegistration registration)
        {
            if (string.IsNullOrWhiteSpace(registration.Username) || string.IsNullOrWhiteSpace(registration.Password))
                return BadRequest("Username and password are required.");

            if (await _context.Users.AnyAsync(u => u.Username == registration.Username))
                return Conflict($"Username '{registration.Username}' is already taken.");

            if (await _context.Users.AnyAsync(u => u.Email == registration.Email))
                return Conflict($"Email '{registration.Email}' is already registered.");

            var user = new User
            {
                Username = registration.Username,
                Email = registration.Email,
                PasswordHash = HashPassword(registration.Password),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"User {user.Username} registered successfully!",
                user.Id,
                user.Email,
                user.CreatedAt
            });
        }

        // Simple SHA256 password hashing (for demo — use a stronger algorithm in production)
        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }

    // DTO (Data Transfer Object)
    public class UserRegistration
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Email { get; set; } = "";
    }
}
