namespace HuddleBackEnd.Models
{
    public class ConversationMember
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public int UserId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public string Role { get; set; } = "member";

        // Navigation
        public Conversation Conversation { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
