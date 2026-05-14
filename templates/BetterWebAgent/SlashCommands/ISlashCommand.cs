using BetterWebAgent.Chats;

namespace BetterWebAgent.SlashCommands;

/// <summary>Outcome of running a slash command.</summary>
/// <param name="Handled">True if the command was matched and processed (skips normal LLM flow).</param>
/// <param name="ChatId">The chatId to associate with the conversation after the command runs.
/// May differ from the input (e.g. /new returns null to clear the active chat,
/// /clear returns the same id).</param>
public record SlashCommandResult(bool Handled, string? ChatId);

/// <summary>
/// A slash command handler. Implementations are discovered via DI:
/// register them as IEnumerable&lt;ISlashCommand&gt; in SetupServices.cs.
/// </summary>
public interface ISlashCommand
{
    /// <summary>The command keyword without the leading slash (e.g. "clear").</summary>
    string Name { get; }

    /// <summary>One-line description shown by /help.</summary>
    string Description { get; }

    Task<SlashCommandResult> ExecuteAsync(string? chatId, string args, IChatHubClient caller);
}
