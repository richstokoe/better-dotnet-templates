using BetterWebAgent.Chats;

namespace BetterWebAgent.SlashCommands;

public class NewCommand : ISlashCommand
{
    public string Name => "new";
    public string Description => "Start a new conversation.";

    public Task<SlashCommandResult> ExecuteAsync(string? chatId, string args, IChatHubClient caller)
    {
        // Returning null tells the caller (and frontend) to drop the current
        // active chatId. The next user message will create a fresh session.
        return Task.FromResult(new SlashCommandResult(true, null));
    }
}
