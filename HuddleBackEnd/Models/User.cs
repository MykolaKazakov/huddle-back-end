namespace HuddleBackEnd.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<ConversationMember> ConversationMembers { get; set; } = new List<ConversationMember>();
        public ICollection<MessageReaction> MessageReactions { get; set; } = new List<MessageReaction>();
    }
}
