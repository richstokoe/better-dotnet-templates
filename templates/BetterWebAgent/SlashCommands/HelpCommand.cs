using System.Text;
using BetterWebAgent.Chats;

namespace BetterWebAgent.SlashCommands;

public class HelpCommand : ISlashCommand
{
    public string Name => "help";
    public string Description => "Show available slash commands.";

    // Resolved lazily to avoid a circular dependency with SlashCommandRegistry.
    private readonly IServiceProvider _services;

    public HelpCommand(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<SlashCommandResult> ExecuteAsync(string? chatId, string args, IChatHubClient caller)
    {
        var registry = (SlashCommandRegistry)_services.GetService(typeof(SlashCommandRegistry))!;
        var sb = new StringBuilder("**Available commands:**\n");
        foreach (var cmd in registry.All.OrderBy(c => c.Name))
            sb.Append($"\n- `/{cmd.Name}` — {cmd.Description}");

        var msg = new ChatMessageDto(Guid.NewGuid().ToString(), chatId ?? "", "system", sb.ToString(), DateTime.UtcNow);
        await caller.ReceiveMessage(chatId ?? "", msg);
        return new SlashCommandResult(true, chatId);
    }
}
