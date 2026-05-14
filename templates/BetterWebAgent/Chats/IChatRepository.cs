namespace BetterWebAgent.Chats;

/// <summary>
/// Storage abstraction for chat sessions and messages.
/// Swap the in-memory implementation for a database-backed one by
/// registering a different IChatRepository in SetupServices.cs.
/// </summary>
public interface IChatRepository
{
    Task<ChatSession> CreateSessionAsync(string title);
    Task<ChatSession?> GetSessionAsync(Guid id);
    Task<IReadOnlyList<ChatSession>> GetAllSessionsAsync();
    Task UpdateSessionTitleAsync(Guid id, string title);
    Task TouchSessionAsync(Guid id);
    Task DeleteSessionAsync(Guid id);

    Task<ChatMessage> AddMessageAsync(Guid sessionId, string source, string content);
    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid sessionId);
    Task ClearMessagesAsync(Guid sessionId);
}
