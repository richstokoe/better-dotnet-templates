namespace BetterWebAgent.Chats;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid ChatSessionId { get; set; }
    public string Source { get; set; } = string.Empty; // "user" | "agent" | "system"
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
