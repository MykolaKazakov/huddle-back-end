using Microsoft.AspNetCore.Mvc;

namespace HuddleBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegistrationController : ControllerBase
    {
        [HttpPost]
        public IActionResult Register([FromBody] UserRegistration registration)
        {
            if (string.IsNullOrWhiteSpace(registration.Username) || string.IsNullOrWhiteSpace(registration.Password))
                return BadRequest("Username and password are required.");

            // Тимчасово просто повертаємо підтвердження
            return Ok(new { message = $"User {registration.Username} registered successfully!" });
        }
    }

    public class UserRegistration
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Email { get; set; } = "";
    }
}
