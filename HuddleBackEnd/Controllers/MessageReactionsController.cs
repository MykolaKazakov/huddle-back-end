using HuddleBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HuddleBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageReactionsController : ControllerBase
    {
        private readonly HuddleDbContext _context;

        public MessageReactionsController(HuddleDbContext context)
        {
            _context = context;
        }

        // GET: api/messagereactions?messageId=1
        [HttpGet]
        public async Task<IActionResult> GetReactions([FromQuery] int messageId)
        {
            var reactions = await _context.MessageReactions
                .Where(r => r.MessageId == messageId)
                .Include(r => r.User)
                .Select(r => new
                {
                    r.Id,
                    r.MessageId,
                    r.UserId,
                    userName = r.User.Username,
                    userAvatar = r.User.AvatarUrl,
                    r.Emoji,
                    r.ReactedAt
                })
                .ToListAsync();

            return Ok(reactions);
        }

        // POST: api/messagereactions
        [HttpPost]
        public async Task<IActionResult> AddReaction([FromBody] AddReactionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Emoji))
                return BadRequest("Emoji is required");

            // Check if message exists
            var message = await _context.Messages.FindAsync(dto.MessageId);
            if (message == null || message.IsDeleted)
                return NotFound("Message not found");

            // Check if user already reacted with this emoji
            var existingReaction = await _context.MessageReactions
                .FirstOrDefaultAsync(r => r.MessageId == dto.MessageId
                    && r.UserId == dto.UserId
                    && r.Emoji == dto.Emoji);

            if (existingReaction != null)
            {
                // Remove reaction if it already exists (toggle behavior)
                _context.MessageReactions.Remove(existingReaction);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Reaction removed", removed = true });
            }

            // Add new reaction
            var reaction = new MessageReaction
            {
                MessageId = dto.MessageId,
                UserId = dto.UserId,
                Emoji = dto.Emoji,
                ReactedAt = DateTime.UtcNow
            };

            _context.MessageReactions.Add(reaction);
            await _context.SaveChangesAsync();

            // Fetch the reaction with user info
            var savedReaction = await _context.MessageReactions
                .Include(r => r.User)
                .FirstAsync(r => r.Id == reaction.Id);

            return CreatedAtAction(nameof(GetReactions), new { messageId = dto.MessageId }, new
            {
                savedReaction.Id,
                savedReaction.MessageId,
                savedReaction.UserId,
                userName = savedReaction.User.Username,
                userAvatar = savedReaction.User.AvatarUrl,
                savedReaction.Emoji,
                savedReaction.ReactedAt,
                message = "Reaction added successfully",
                removed = false
            });
        }

        // DELETE: api/messagereactions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveReaction(int id, [FromQuery] int userId)
        {
            var reaction = await _context.MessageReactions.FindAsync(id);
            if (reaction == null)
                return NotFound();

            if (reaction.UserId != userId)
                return Forbid();

            _context.MessageReactions.Remove(reaction);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Reaction removed successfully" });
        }

        // GET: api/messagereactions/grouped?messageId=1
        [HttpGet("grouped")]
        public async Task<IActionResult> GetGroupedReactions([FromQuery] int messageId)
        {
            var reactions = await _context.MessageReactions
                .Where(r => r.MessageId == messageId)
                .Include(r => r.User)
                .ToListAsync();

            var grouped = reactions
                .GroupBy(r => r.Emoji)
                .Select(g => new
                {
                    emoji = g.Key,
                    count = g.Count(),
                    users = g.Select(r => new
                    {
                        r.UserId,
                        userName = r.User.Username,
                        userAvatar = r.User.AvatarUrl
                    }).ToList()
                })
                .ToList();

            return Ok(grouped);
        }
    }

    // DTOs
    public class AddReactionDto
    {
        public int MessageId { get; set; }
        public int UserId { get; set; }
        public string Emoji { get; set; } = "";
    }
}
