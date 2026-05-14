using BetterWebAgent.Chats;

namespace BetterWebAgent.SlashCommands;

public class ClearCommand : ISlashCommand
{
    public string Name => "clear";
    public string Description => "Clear the message history for the current conversation.";

    private readonly IChatRepository _chats;

    public ClearCommand(IChatRepository chats)
    {
        _chats = chats;
    }

    public async Task<SlashCommandResult> ExecuteAsync(string? chatId, string args, IChatHubClient caller)
    {
        if (chatId == null || !Guid.TryParse(chatId, out var id))
        {
            var notice = new ChatMessageDto(Guid.NewGuid().ToString(), "", "system",
                "No active conversation to clear.", DateTime.UtcNow);
            await caller.ReceiveMessage("", notice);
            return new SlashCommandResult(true, chatId);
        }

        await _chats.ClearMessagesAsync(id);
        await caller.ChatCleared(chatId);
        return new SlashCommandResult(true, chatId);
    }
}
