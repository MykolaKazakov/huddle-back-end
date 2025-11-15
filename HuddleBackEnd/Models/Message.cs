namespace HuddleBackEnd.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string? Content { get; set; }
        public string? AttachmentUrl { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? EditedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation
        public Conversation Conversation { get; set; } = null!;
        public User Sender { get; set; } = null!;
        public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
    }
}
