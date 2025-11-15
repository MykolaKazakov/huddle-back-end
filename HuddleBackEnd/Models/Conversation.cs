namespace HuddleBackEnd.Models
{
    public class Conversation
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsGroup { get; set; } = false;
        public int CreatedById { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User CreatedBy { get; set; } = null!;
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<ConversationMember> Members { get; set; } = new List<ConversationMember>();
    }
}
