namespace BetterWebAgent.Agents;

/// <summary>
/// Storage abstraction for long-running agent tasks. Swap the in-memory
/// implementation for a database-backed one by registering a different
/// IAgentTaskRepository in SetupServices.cs.
/// </summary>
public interface IAgentTaskRepository
{
    Task<AgentTask> CreateAsync(string prompt, string title, Guid? chatSessionId);
    Task<AgentTask?> GetAsync(Guid id);
    Task<IReadOnlyList<AgentTask>> GetAllAsync();
    Task UpdateStatusAsync(Guid id, AgentTaskStatus status, DateTime? startedAt = null, DateTime? completedAt = null, string? output = null);
    Task DeleteAsync(Guid id);
    Task<IReadOnlyList<Guid>> ClearCompletedAsync();
}
