using System.Collections.Concurrent;

namespace BetterWebAgent.Chats;

public class InMemoryChatRepository : IChatRepository
{
    private readonly ConcurrentDictionary<Guid, ChatSession> _sessions = new();
    private readonly ConcurrentDictionary<Guid, List<ChatMessage>> _messages = new();

    public Task<ChatSession> CreateSessionAsync(string title)
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            Title = title,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _sessions[session.Id] = session;
        _messages[session.Id] = [];
        return Task.FromResult(session);
    }

    public Task<ChatSession?> GetSessionAsync(Guid id)
    {
        _sessions.TryGetValue(id, out var session);
        return Task.FromResult(session);
    }

    public Task<IReadOnlyList<ChatSession>> GetAllSessionsAsync()
    {
        var list = _sessions.Values.OrderByDescending(s => s.UpdatedAt).ToList();
        return Task.FromResult<IReadOnlyList<ChatSession>>(list);
    }

    public Task UpdateSessionTitleAsync(Guid id, string title)
    {
        if (_sessions.TryGetValue(id, out var session))
        {
            session.Title = title;
            session.UpdatedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task TouchSessionAsync(Guid id)
    {
        if (_sessions.TryGetValue(id, out var session))
            session.UpdatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task DeleteSessionAsync(Guid id)
    {
        _sessions.TryRemove(id, out _);
        _messages.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<ChatMessage> AddMessageAsync(Guid sessionId, string source, string content)
    {
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatSessionId = sessionId,
            Source = source,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
        var list = _messages.GetOrAdd(sessionId, _ => []);
        lock (list) { list.Add(message); }
        return Task.FromResult(message);
    }

    public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid sessionId)
    {
        if (!_messages.TryGetValue(sessionId, out var list))
            return Task.FromResult<IReadOnlyList<ChatMessage>>([]);
        lock (list) { return Task.FromResult<IReadOnlyList<ChatMessage>>([.. list]); }
    }

    public Task ClearMessagesAsync(Guid sessionId)
    {
        if (_messages.TryGetValue(sessionId, out var list))
            lock (list) { list.Clear(); }
        return Task.CompletedTask;
    }
}
