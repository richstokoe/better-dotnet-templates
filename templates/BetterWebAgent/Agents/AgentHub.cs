using Microsoft.AspNetCore.SignalR;

namespace BetterWebAgent.Agents;

public interface IAgentHubClient
{
    Task AllTasks(IEnumerable<AgentTaskDto> tasks);
    Task TaskCreated(AgentTaskDto task);
    Task TaskUpdated(AgentTaskDto task);
    Task TaskDeleted(string taskId);
}

public record AgentTaskDto(
    string Id,
    string Title,
    string Prompt,
    string Status,
    string? Output,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? ChatSessionId);

public class AgentHub : Hub<IAgentHubClient>
{
    private readonly IAgentTaskRepository _repository;
    private readonly AgentTaskRunner _runner;

    public AgentHub(IAgentTaskRepository repository, AgentTaskRunner runner)
    {
        _repository = repository;
        _runner = runner;
    }

    public override async Task OnConnectedAsync()
    {
        var tasks = await _repository.GetAllAsync();
        await Clients.Caller.AllTasks(tasks.Select(ToDto));
        await base.OnConnectedAsync();
    }

    public async Task StopTask(string taskId)
    {
        if (!Guid.TryParse(taskId, out var id)) return;
        await _runner.StopAsync(id);
    }

    public async Task DeleteTask(string taskId)
    {
        if (!Guid.TryParse(taskId, out var id)) return;
        await _runner.DeleteAsync(id);
    }

    public async Task ClearCompletedTasks()
    {
        var deleted = await _repository.ClearCompletedAsync();
        foreach (var id in deleted)
            await Clients.All.TaskDeleted(id.ToString());
    }

    internal static AgentTaskDto ToDto(AgentTask t) =>
        new(t.Id.ToString(),
            t.Title,
            t.Prompt,
            t.Status.ToString(),
            t.Output,
            t.CreatedAt,
            t.StartedAt,
            t.CompletedAt,
            t.ChatSessionId?.ToString());
}
