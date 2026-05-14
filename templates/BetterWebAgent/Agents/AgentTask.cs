namespace BetterWebAgent.Agents;

public enum AgentTaskStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

public class AgentTask
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public AgentTaskStatus Status { get; set; }
    public string? Output { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>The chat session that triggered this task. Used to post the result back when complete.</summary>
    public Guid? ChatSessionId { get; set; }
}
