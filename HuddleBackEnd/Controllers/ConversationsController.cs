using HuddleBackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HuddleBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConversationsController : ControllerBase
    {
        private readonly HuddleDbContext _context;

        public ConversationsController(HuddleDbContext context)
        {
            _context = context;
        }

        // GET: api/conversations?userId=1
        [HttpGet]
        public async Task<IActionResult> GetConversations([FromQuery] int userId)
        {
            var conversations = await _context.ConversationMembers
                .Where(cm => cm.UserId == userId)
                .Include(cm => cm.Conversation)
                    .ThenInclude(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .Include(cm => cm.Conversation)
                    .ThenInclude(c => c.Members)
                        .ThenInclude(m => m.User)
                .Select(cm => new
                {
                    id = cm.Conversation.Id,
                    name = cm.Conversation.IsGroup
                        ? cm.Conversation.Name
                        : cm.Conversation.Members
                            .Where(m => m.UserId != userId)
                            .Select(m => m.User.Username)
                            .FirstOrDefault(),
                    isGroup = cm.Conversation.IsGroup,
                    lastMessage = cm.Conversation.Messages
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => m.Content)
                        .FirstOrDefault(),
                    lastMessageTime = cm.Conversation.Messages
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => m.SentAt)
                        .FirstOrDefault(),
                    members = cm.Conversation.Members.Select(m => new
                    {
                        m.UserId,
                        m.User.Username,
                        m.User.AvatarUrl,
                        m.Role
                    }).ToList(),
                    createdAt = cm.Conversation.CreatedAt
                })
                .OrderByDescending(c => c.lastMessageTime)
                .ToListAsync();

            return Ok(conversations);
        }

        // GET: api/conversations/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetConversation(int id)
        {
            var conversation = await _context.Conversations
                .Include(c => c.Members)
                    .ThenInclude(m => m.User)
                .Include(c => c.Messages)
                    .ThenInclude(m => m.Sender)
                .Include(c => c.Messages)
                    .ThenInclude(m => m.Reactions)
                        .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (conversation == null)
                return NotFound();

            return Ok(new
            {
                conversation.Id,
                conversation.Name,
                conversation.IsGroup,
                conversation.CreatedAt,
                members = conversation.Members.Select(m => new
                {
                    m.UserId,
                    m.User.Username,
                    m.User.Email,
                    m.User.AvatarUrl,
                    m.Role,
                    m.JoinedAt
                }),
                messages = conversation.Messages
                    .Where(m => !m.IsDeleted)
                    .OrderBy(m => m.SentAt)
                    .Select(m => new
                    {
                        m.Id,
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
            });
        }

        // POST: api/conversations
        [HttpPost]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationDto dto)
        {
            if (dto.MemberIds == null || dto.MemberIds.Count < 2)
                return BadRequest("A conversation must have at least 2 members.");

            // For direct chats, check if conversation already exists
            if (!dto.IsGroup && dto.MemberIds.Count == 2)
            {
                var existingConversation = await _context.Conversations
                    .Where(c => !c.IsGroup && c.Members.Count == 2)
                    .Where(c => c.Members.Any(m => m.UserId == dto.MemberIds[0]))
                    .Where(c => c.Members.Any(m => m.UserId == dto.MemberIds[1]))
                    .FirstOrDefaultAsync();

                if (existingConversation != null)
                    return Ok(new { id = existingConversation.Id, message = "Conversation already exists" });
            }

            var conversation = new Conversation
            {
                Name = dto.Name,
                IsGroup = dto.IsGroup,
                CreatedById = dto.CreatedById,
                CreatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            // Add members
            foreach (var memberId in dto.MemberIds)
            {
                var member = new ConversationMember
                {
                    ConversationId = conversation.Id,
                    UserId = memberId,
                    Role = memberId == dto.CreatedById ? "admin" : "member",
                    JoinedAt = DateTime.UtcNow
                };
                _context.ConversationMembers.Add(member);
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetConversation), new { id = conversation.Id }, new
            {
                conversation.Id,
                conversation.Name,
                conversation.IsGroup,
                conversation.CreatedAt,
                message = "Conversation created successfully"
            });
        }

        // PUT: api/conversations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateConversation(int id, [FromBody] UpdateConversationDto dto)
        {
            var conversation = await _context.Conversations.FindAsync(id);
            if (conversation == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.Name))
                conversation.Name = dto.Name;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Conversation updated successfully" });
        }

        // POST: api/conversations/5/members
        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMember(int id, [FromBody] AddMemberDto dto)
        {
            var conversation = await _context.Conversations.FindAsync(id);
            if (conversation == null)
                return NotFound();

            if (!conversation.IsGroup)
                return BadRequest("Cannot add members to direct conversations");

            var existingMember = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == id && cm.UserId == dto.UserId);

            if (existingMember != null)
                return Conflict("User is already a member of this conversation");

            var member = new ConversationMember
            {
                ConversationId = id,
                UserId = dto.UserId,
                Role = "member",
                JoinedAt = DateTime.UtcNow
            };

            _context.ConversationMembers.Add(member);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Member added successfully" });
        }

        // DELETE: api/conversations/5/members/3
        [HttpDelete("{id}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(int id, int userId)
        {
            var member = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == id && cm.UserId == userId);

            if (member == null)
                return NotFound();

            _context.ConversationMembers.Remove(member);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Member removed successfully" });
        }
    }

    // DTOs
    public class CreateConversationDto
    {
        public string? Name { get; set; }
        public bool IsGroup { get; set; }
        public int CreatedById { get; set; }
        public List<int> MemberIds { get; set; } = new();
    }

    public class UpdateConversationDto
    {
        public string? Name { get; set; }
    }

    public class AddMemberDto
    {
        public int UserId { get; set; }
    }
}
