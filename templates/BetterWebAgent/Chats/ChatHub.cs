using BetterWebAgent.Agents;
using BetterWebAgent.SlashCommands;
using Microsoft.AspNetCore.SignalR;

namespace BetterWebAgent.Chats;

public interface IChatHubClient
{
    Task AllChats(IEnumerable<ChatSessionDto> sessions);
    Task ChatCreated(ChatSessionDto session);
    Task ChatUpdated(ChatSessionDto session);
    Task ChatDeleted(string chatId);
    Task ChatHistory(string chatId, IEnumerable<ChatMessageDto> messages);
    Task ChatCleared(string chatId);

    /// <summary>
    /// Sent to the caller as soon as a new session is created on their behalf, so
    /// the client can adopt the new chatId before any ReceiveMessage / ReceiveStreamToken
    /// events arrive. Without this, the client filters out events for the new chat
    /// because SendMessage doesn't return its chatId until the full response stream
    /// finishes — too late.
    /// </summary>
    Task ChatActivated(string chatId);

    Task ReceiveMessage(string chatId, ChatMessageDto message);
    Task ReceiveStreamToken(string chatId, string token);
    Task StreamComplete(string chatId);
}

public record ChatSessionDto(string Id, string Title, DateTime CreatedAt, DateTime UpdatedAt);
public record ChatMessageDto(string Id, string ChatSessionId, string Source, string Content, DateTime CreatedAt);

public class ChatHub : Hub<IChatHubClient>
{
    private readonly IChatRepository _chats;
    private readonly WebAgentFactory _agentFactory;
    private readonly AgentTaskRunner _taskRunner;
    private readonly SlashCommandRegistry _slashCommands;
    private readonly ILogger<ChatHub> _logger;

    // Limit history sent to the model to keep context size manageable.
    private const int MaxHistoryMessages = 40;

    public ChatHub(
        IChatRepository chats,
        WebAgentFactory agentFactory,
        AgentTaskRunner taskRunner,
        SlashCommandRegistry slashCommands,
        ILogger<ChatHub> logger)
    {
        _chats = chats;
        _agentFactory = agentFactory;
        _taskRunner = taskRunner;
        _slashCommands = slashCommands;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var sessions = await _chats.GetAllSessionsAsync();
        await Clients.Caller.AllChats(sessions.Select(ToDto));
        await base.OnConnectedAsync();
    }

    public async Task GetChatHistory(string chatId)
    {
        if (!Guid.TryParse(chatId, out var id)) return;
        var messages = await _chats.GetMessagesAsync(id);
        await Clients.Caller.ChatHistory(chatId, messages.Select(ToMessageDto));
    }

    public async Task DeleteChat(string chatId)
    {
        if (!Guid.TryParse(chatId, out var id)) return;
        await _chats.DeleteSessionAsync(id);
        await Clients.All.ChatDeleted(chatId);
    }

    /// <summary>
    /// Send a message. If chatId is null a new session is created.
    /// Returns the chatId (existing or newly created) so the client can track the session.
    /// </summary>
    public async Task<string?> SendMessage(string? chatId, string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return chatId;

        // Slash commands intercept here. They may create/clear sessions and either
        // short-circuit (returning a new/null chatId) or fall through to normal chat.
        if (message.StartsWith('/'))
        {
            var result = await _slashCommands.TryExecuteAsync(chatId, message, Clients.Caller);
            if (result.Handled) return result.ChatId;
        }

        var session = await GetOrCreateSessionAsync(chatId, message);

        await _chats.AddMessageAsync(session.Id, "user", message);
        await _chats.TouchSessionAsync(session.Id);
        await Clients.Caller.ReceiveMessage(session.Id.ToString(),
            new ChatMessageDto(Guid.NewGuid().ToString(), session.Id.ToString(), "user", message, DateTime.UtcNow));

        // Decide whether this prompt is "agentic" (likely long-running) or a "direct" chat reply.
        var classification = await _agentFactory.ClassifyAsync(message);
        if (classification.IsAgentic)
        {
            var task = await _taskRunner.CreateAndStartAsync(message, classification.Title, session.Id);
            var reply = $"I've started a background task: **{task.Title}**. The result will appear here when it's done.";
            await _chats.AddMessageAsync(session.Id, "agent", reply);
            await Clients.Caller.ReceiveMessage(session.Id.ToString(),
                new ChatMessageDto(Guid.NewGuid().ToString(), session.Id.ToString(), "agent", reply, DateTime.UtcNow));
            await Clients.Caller.StreamComplete(session.Id.ToString());
            return session.Id.ToString();
        }

        // Direct response — stream tokens back to the caller.
        var history = (await _chats.GetMessagesAsync(session.Id))
            .Where(m => m.Source != "system")
            .TakeLast(MaxHistoryMessages)
            .Select(m => (m.Source, m.Content));

        var agent = await _agentFactory.CreateAsync();
        var outputBuilder = new System.Text.StringBuilder();

        try
        {
            await foreach (var token in agent.StreamResponseAsync(history))
            {
                outputBuilder.Append(token);
                await Clients.Caller.ReceiveStreamToken(session.Id.ToString(), token);
            }
            await _chats.AddMessageAsync(session.Id, "agent", outputBuilder.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM error for chat {ChatId}", session.Id);
            var errorMsg = $"⚠️ Error communicating with the AI: {ex.GetBaseException().Message}";
            await Clients.Caller.ReceiveMessage(session.Id.ToString(),
                new ChatMessageDto(Guid.NewGuid().ToString(), session.Id.ToString(), "system", errorMsg, DateTime.UtcNow));
        }
        finally
        {
            await Clients.Caller.StreamComplete(session.Id.ToString());
        }

        return session.Id.ToString();
    }

    private async Task<ChatSession> GetOrCreateSessionAsync(string? chatId, string firstMessage)
    {
        if (chatId != null && Guid.TryParse(chatId, out var existingId))
        {
            var existing = await _chats.GetSessionAsync(existingId);
            if (existing != null) return existing;
        }

        // Provisional title (first 60 chars). The agent factory then generates a better
        // title in the background and the UI gets a ChatUpdated event when ready.
        var provisionalTitle = firstMessage.Length > 60 ? firstMessage[..60] + "…" : firstMessage;
        var session = await _chats.CreateSessionAsync(provisionalTitle);
        await Clients.All.ChatCreated(ToDto(session));

        // Tell the caller to adopt this chatId immediately so subsequent
        // ReceiveMessage / ReceiveStreamToken events for it aren't filtered out.
        await Clients.Caller.ChatActivated(session.Id.ToString());

        _ = GenerateTitleInBackgroundAsync(session.Id, firstMessage);
        return session;
    }

    private async Task GenerateTitleInBackgroundAsync(Guid sessionId, string firstMessage)
    {
        try
        {
            var title = await _agentFactory.GenerateTitleAsync(firstMessage);
            if (string.IsNullOrWhiteSpace(title)) return;

            await _chats.UpdateSessionTitleAsync(sessionId, title);
            var updated = await _chats.GetSessionAsync(sessionId);
            if (updated != null)
                await Clients.All.ChatUpdated(ToDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate title for chat {ChatId}", sessionId);
        }
    }

    internal static ChatSessionDto ToDto(ChatSession s) =>
        new(s.Id.ToString(), s.Title, s.CreatedAt, s.UpdatedAt);

    internal static ChatMessageDto ToMessageDto(ChatMessage m) =>
        new(m.Id.ToString(), m.ChatSessionId.ToString(), m.Source, m.Content, m.CreatedAt);
}
