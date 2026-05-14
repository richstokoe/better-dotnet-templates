using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace BetterWebAgent.Agents;

/// <summary>
/// A thin wrapper around <see cref="AIAgent"/> that handles streaming responses
/// with either a single prompt or a full conversation history. Tools registered
/// via <see cref="RichStokoe.AgentTools.ToolManager"/> are available to the model
/// on every turn. The system prompt instructs the model to wrap reasoning in
/// &lt;think&gt;...&lt;/think&gt; tags so the frontend can render it as a collapsible
/// chain-of-thought block.
/// </summary>
public class WebAgent
{
    public const string SystemInstructions = """
        You are a helpful AI assistant.

        Guidelines:
        - Be direct, clear, and accurate.
        - Use Markdown for formatting (lists, code fences, tables).
        - When reasoning through a problem, wrap your private reasoning in
          <think>...</think> tags BEFORE writing your final answer. The UI
          renders these as a collapsible chain-of-thought block.
        - Keep the answer after </think> focused and free of meta-commentary.
        - Use the tools available to you when they would help answer accurately.
        """;

    private readonly AIAgent _agent;

    public WebAgent(AIAgent agent)
    {
        _agent = agent;
    }

    /// <summary>Stream a response to a single prompt with no prior history.</summary>
    public async IAsyncEnumerable<string> StreamResponseAsync(string prompt)
    {
        await foreach (var update in _agent.RunStreamingAsync(prompt))
        {
            if (!string.IsNullOrEmpty(update.Text))
                yield return update.Text;
        }
    }

    /// <summary>Stream a response using a full conversation history.</summary>
    public async IAsyncEnumerable<string> StreamResponseAsync(
        IEnumerable<(string Source, string Content)> history)
    {
        var messages = history.Select(h => new ChatMessage(
            h.Source == "user" ? ChatRole.User : ChatRole.Assistant,
            h.Content)).ToList();

        await foreach (var update in _agent.RunStreamingAsync(messages))
        {
            if (!string.IsNullOrEmpty(update.Text))
                yield return update.Text;
        }
    }

    /// <summary>One-shot non-streaming response (used by classification and title generation).</summary>
    public async Task<string> GetResponseAsync(string prompt)
    {
        var response = await _agent.RunAsync(prompt);
        return response.Text ?? string.Empty;
    }
}
