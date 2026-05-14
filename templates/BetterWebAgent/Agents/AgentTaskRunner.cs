using System.Collections.Concurrent;
using System.Text;
using BetterWebAgent.Chats;
using Microsoft.AspNetCore.SignalR;

namespace BetterWebAgent.Agents;

/// <summary>
/// Singleton that runs agentic tasks in the background.
/// Streams progress via the AgentHub and posts the final result back into the
/// originating chat session via the ChatHub.
/// </summary>
public class AgentTaskRunner
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _running = new();
    private readonly IAgentTaskRepository _repository;
    private readonly WebAgentFactory _factory;
    private readonly IChatRepository _chats;
    private readonly IHubContext<AgentHub, IAgentHubClient> _agentHub;
    private readonly IHubContext<ChatHub, IChatHubClient> _chatHub;
    private readonly ILogger<AgentTaskRunner> _logger;

    public AgentTaskRunner(
        IAgentTaskRepository repository,
        WebAgentFactory factory,
        IChatRepository chats,
        IHubContext<AgentHub, IAgentHubClient> agentHub,
        IHubContext<ChatHub, IChatHubClient> chatHub,
        ILogger<AgentTaskRunner> logger)
    {
        _repository = repository;
        _factory = factory;
        _chats = chats;
        _agentHub = agentHub;
        _chatHub = chatHub;
        _logger = logger;
    }

    public async Task<AgentTask> CreateAndStartAsync(string prompt, string title, Guid? chatSessionId = null)
    {
        var task = await _repository.CreateAsync(prompt, title, chatSessionId);
        await _agentHub.Clients.All.TaskCreated(AgentHub.ToDto(task));
        _ = RunAsync(task);
        return task;
    }

    public async Task StopAsync(Guid taskId)
    {
        if (_running.TryRemove(taskId, out var cts))
            cts.Cancel();

        await _repository.UpdateStatusAsync(taskId, AgentTaskStatus.Cancelled, completedAt: DateTime.UtcNow);
        var updated = await _repository.GetAsync(taskId);
        if (updated != null)
            await _agentHub.Clients.All.TaskUpdated(AgentHub.ToDto(updated));
    }

    public async Task DeleteAsync(Guid taskId)
    {
        if (_running.TryRemove(taskId, out var cts))
            cts.Cancel();

        await _repository.DeleteAsync(taskId);
        await _agentHub.Clients.All.TaskDeleted(taskId.ToString());
    }

    private async Task RunAsync(AgentTask task)
    {
        var cts = new CancellationTokenSource();
        _running[task.Id] = cts;

        await _repository.UpdateStatusAsync(task.Id, AgentTaskStatus.Running, startedAt: DateTime.UtcNow);
        var updated = await _repository.GetAsync(task.Id);
        if (updated != null)
            await _agentHub.Clients.All.TaskUpdated(AgentHub.ToDto(updated));

        try
        {
            var agent = await _factory.CreateAsync();
            var output = new StringBuilder();

            await foreach (var token in agent.StreamResponseAsync(task.Prompt))
            {
                if (cts.Token.IsCancellationRequested) break;
                output.Append(token);
            }

            if (cts.Token.IsCancellationRequested) return;

            var finalOutput = output.ToString();
            await _repository.UpdateStatusAsync(
                task.Id, AgentTaskStatus.Completed,
                completedAt: DateTime.UtcNow,
                output: finalOutput);

            // Post the result back to the originating chat session so the user
            // sees it inline in their conversation.
            if (task.ChatSessionId.HasValue)
            {
                var chatIdStr = task.ChatSessionId.Value.ToString();
                var reply = $"**Task completed: {task.Title}**\n\n{finalOutput}";
                await _chats.AddMessageAsync(task.ChatSessionId.Value, "agent", reply);
                await _chatHub.Clients.All.ReceiveMessage(chatIdStr,
                    new ChatMessageDto(Guid.NewGuid().ToString(), chatIdStr, "agent", reply, DateTime.UtcNow));
            }
        }
        catch (OperationCanceledException)
        {
            // Already handled by StopAsync.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentTask {TaskId} failed", task.Id);
            await _repository.UpdateStatusAsync(task.Id, AgentTaskStatus.Failed, completedAt: DateTime.UtcNow);
        }
        finally
        {
            _running.TryRemove(task.Id, out _);
            var final = await _repository.GetAsync(task.Id);
            if (final != null)
                await _agentHub.Clients.All.TaskUpdated(AgentHub.ToDto(final));
        }
    }
}
