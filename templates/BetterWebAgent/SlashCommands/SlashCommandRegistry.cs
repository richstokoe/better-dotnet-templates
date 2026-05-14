using BetterWebAgent.Chats;

namespace BetterWebAgent.SlashCommands;

/// <summary>
/// Looks up registered slash commands by name and dispatches the first matching one.
/// Names are case-insensitive. Unknown commands fall through to normal chat.
/// </summary>
public class SlashCommandRegistry
{
    private readonly Dictionary<string, ISlashCommand> _commands;

    public IReadOnlyCollection<ISlashCommand> All => _commands.Values;

    public SlashCommandRegistry(IEnumerable<ISlashCommand> commands)
    {
        _commands = commands.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<SlashCommandResult> TryExecuteAsync(string? chatId, string message, IChatHubClient caller)
    {
        // Strip the leading slash and split into command + args.
        var trimmed = message.AsSpan(1);
        var spaceIndex = trimmed.IndexOf(' ');
        var name = (spaceIndex < 0 ? trimmed : trimmed[..spaceIndex]).ToString();
        var args = spaceIndex < 0 ? "" : trimmed[(spaceIndex + 1)..].ToString();

        if (!_commands.TryGetValue(name, out var command))
            return new SlashCommandResult(false, chatId);

        return await command.ExecuteAsync(chatId, args, caller);
    }
}
