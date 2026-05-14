using System.Collections.Concurrent;

namespace BetterWebAgent.Agents;

public class InMemoryAgentTaskRepository : IAgentTaskRepository
{
    private readonly ConcurrentDictionary<Guid, AgentTask> _tasks = new();

    public Task<AgentTask> CreateAsync(string prompt, string title, Guid? chatSessionId)
    {
        var task = new AgentTask
        {
            Id = Guid.NewGuid(),
            Title = title,
            Prompt = prompt,
            Status = AgentTaskStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ChatSessionId = chatSessionId
        };
        _tasks[task.Id] = task;
        return Task.FromResult(task);
    }

    public Task<AgentTask?> GetAsync(Guid id)
    {
        _tasks.TryGetValue(id, out var task);
        return Task.FromResult(task);
    }

    public Task<IReadOnlyList<AgentTask>> GetAllAsync()
    {
        var list = _tasks.Values.OrderByDescending(t => t.CreatedAt).ToList();
        return Task.FromResult<IReadOnlyList<AgentTask>>(list);
    }

    public Task UpdateStatusAsync(Guid id, AgentTaskStatus status, DateTime? startedAt = null, DateTime? completedAt = null, string? output = null)
    {
        if (_tasks.TryGetValue(id, out var task))
        {
            task.Status = status;
            if (startedAt.HasValue) task.StartedAt = startedAt;
            if (completedAt.HasValue) task.CompletedAt = completedAt;
            if (output != null) task.Output = output;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _tasks.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Guid>> ClearCompletedAsync()
    {
        var ids = _tasks.Values
            .Where(t => t.Status is AgentTaskStatus.Completed or AgentTaskStatus.Failed or AgentTaskStatus.Cancelled)
            .Select(t => t.Id)
            .ToList();
        foreach (var id in ids) _tasks.TryRemove(id, out _);
        return Task.FromResult<IReadOnlyList<Guid>>(ids);
    }
}
