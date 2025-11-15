using HuddleBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HuddleBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly HuddleDbContext _context;

        public UsersController(HuddleDbContext context)
        {
            _context = context;
        }

        // GET: api/users
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string? search = null, [FromQuery] int? excludeUserId = null)
        {
            var query = _context.Users.AsQueryable();

            // Search by username or email
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search));
            }

            // Exclude specific user (useful for getting "other users")
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            var users = await query
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.AvatarUrl,
                    u.CreatedAt
                })
                .OrderBy(u => u.Username)
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/users/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.AvatarUrl,
                    u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        // PUT: api/users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.Username))
            {
                // Check if username is already taken by another user
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == dto.Username && u.Id != id);
                if (existingUser != null)
                    return Conflict("Username is already taken");

                user.Username = dto.Username;
            }

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                // Check if email is already taken by another user
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Id != id);
                if (existingUser != null)
                    return Conflict("Email is already registered");

                user.Email = dto.Email;
            }

            if (dto.AvatarUrl != null)
            {
                user.AvatarUrl = dto.AvatarUrl;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.AvatarUrl,
                message = "User updated successfully"
            });
        }
    }

    // DTOs
    public class UpdateUserDto
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
