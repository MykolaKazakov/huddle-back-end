using HuddleBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HuddleBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly HuddleDbContext _context;

        public MessagesController(HuddleDbContext context)
        {
            _context = context;
        }

        // GET: api/messages?conversationId=1
        [HttpGet]
        public async Task<IActionResult> GetMessages([FromQuery] int conversationId, [FromQuery] int limit = 50, [FromQuery] int offset = 0)
        {
            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                .Include(m => m.Sender)
                .Include(m => m.Reactions)
                    .ThenInclude(r => r.User)
                .OrderByDescending(m => m.SentAt)
                .Skip(offset)
                .Take(limit)
                .Select(m => new
                {
                    m.Id,
                    m.ConversationId,
                    m.SenderId,
                    senderName = m.Sender.Username,
                    senderAvatar = m.Sender.AvatarUrl,
                    m.Content,
                    m.AttachmentUrl,
                    m.SentAt,
                    m.EditedAt,
                    reactions = m.Reactions.Select(r => new
                    {
                        r.Id,
                        r.UserId,
                        userName = r.User.Username,
                        r.Emoji,
                        r.ReactedAt
                    })
                })
                .ToListAsync();

            return Ok(messages.OrderBy(m => m.SentAt));
        }

        // GET: api/messages/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMessage(int id)
        {
            var message = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Reactions)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (message == null)
                return NotFound();

            return Ok(new
            {
                message.Id,
                message.ConversationId,
                message.SenderId,
                senderName = message.Sender.Username,
                senderAvatar = message.Sender.AvatarUrl,
                message.Content,
                message.AttachmentUrl,
                message.SentAt,
                message.EditedAt,
                reactions = message.Reactions.Select(r => new
                {
                    r.Id,
                    r.UserId,
                    userName = r.User.Username,
                    r.Emoji,
                    r.ReactedAt
                })
            });
        }

        // POST: api/messages
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content) && string.IsNullOrWhiteSpace(dto.AttachmentUrl))
                return BadRequest("Message must have content or attachment");

            // Verify conversation exists and user is a member
            var isMember = await _context.ConversationMembers
                .AnyAsync(cm => cm.ConversationId == dto.ConversationId && cm.UserId == dto.SenderId);

            if (!isMember)
                return Forbidden("User is not a member of this conversation");

            var message = new Message
            {
                ConversationId = dto.ConversationId,
                SenderId = dto.SenderId,
                Content = dto.Content,
                AttachmentUrl = dto.AttachmentUrl,
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Fetch the message with sender info
            var savedMessage = await _context.Messages
                .Include(m => m.Sender)
                .FirstAsync(m => m.Id == message.Id);

            return CreatedAtAction(nameof(GetMessage), new { id = message.Id }, new
            {
                savedMessage.Id,
                savedMessage.ConversationId,
                savedMessage.SenderId,
                senderName = savedMessage.Sender.Username,
                senderAvatar = savedMessage.Sender.AvatarUrl,
                savedMessage.Content,
                savedMessage.AttachmentUrl,
                savedMessage.SentAt,
                message = "Message sent successfully"
            });
        }

        // PUT: api/messages/5
        [HttpPut("{id}")]
        public async Task<IActionResult> EditMessage(int id, [FromBody] EditMessageDto dto)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null || message.IsDeleted)
                return NotFound();

            if (message.SenderId != dto.SenderId)
                return Forbid();

            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest("Content cannot be empty");

            message.Content = dto.Content;
            message.EditedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message.Id,
                message.Content,
                message.EditedAt,
                message = "Message edited successfully"
            });
        }

        // DELETE: api/messages/5?userId=1
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(int id, [FromQuery] int userId)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null || message.IsDeleted)
                return NotFound();

            if (message.SenderId != userId)
                return Forbid();

            message.IsDeleted = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Message deleted successfully" });
        }

        private ForbidResult Forbidden(string message)
        {
            return Forbid();
        }
    }

    // DTOs
    public class SendMessageDto
    {
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string? Content { get; set; }
        public string? AttachmentUrl { get; set; }
    }

    public class EditMessageDto
    {
        public int SenderId { get; set; }
        public string Content { get; set; } = "";
    }
}
