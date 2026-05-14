using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using RichStokoe.AgentTools;

namespace BetterWebAgent.Agents;

public record ClassificationResult(bool IsAgentic, string Title);

/// <summary>
/// Builds <see cref="WebAgent"/> instances backed by Microsoft.Agents.AI and the
/// RichStokoe.AgentTools tool discovery system. Also exposes two auxiliary LLM
/// calls used by the ChatHub: classifying whether a prompt should be promoted
/// to a background task, and generating a concise title for a new conversation.
///
/// Meta-calls (classification, title generation) go through the raw IChatClient
/// with their own focused system prompts — they bypass the agent's chain-of-thought
/// instructions so the responses don't include &lt;think&gt; tags or tool calls.
/// </summary>
public class WebAgentFactory
{
    private readonly IChatClient _chatClient;
    private readonly ToolManager _toolManager;
    private readonly ILogger<WebAgentFactory> _logger;
    private readonly Lazy<AIAgent> _agent;

    private const string ClassifierSystemPrompt = """
        You classify a user request as either "direct" or "agentic".
          direct  — a simple question or task answerable in one response.
          agentic — complex, multi-step, or long-running work (research across many sources,
                    compiling a detailed report, monitoring, multi-action sequences, etc.).
        Reply with a single JSON object and nothing else. No prose, no <think> blocks.
        Examples:
          {"type":"direct"}
          {"type":"agentic","title":"Brief task title (max 60 chars)"}
        """;

    private const string TitleSystemPrompt = """
        You generate short conversation titles. Reply with ONLY the title text — no quotes,
        no trailing punctuation, no preamble, no <think> blocks. Maximum 50 characters.
        """;

    private static readonly Regex ThinkBlockRegex =
        new("<think>.*?</think>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

    public WebAgentFactory(IChatClient chatClient, ToolManager toolManager, ILogger<WebAgentFactory> logger)
    {
        _chatClient = chatClient;
        _toolManager = toolManager;
        _logger = logger;

        // The agent is constructed lazily on first use so the IChatClient and
        // ToolManager are fully wired up before AsAIAgent walks them.
        _agent = new Lazy<AIAgent>(() => _chatClient.AsAIAgent(
            name: "BetterWebAgent",
            instructions: WebAgent.SystemInstructions,
            tools: _toolManager.GetTools()));
    }

    public Task<WebAgent> CreateAsync() => Task.FromResult(new WebAgent(_agent.Value));

    public async Task<ClassificationResult> ClassifyAsync(string prompt)
    {
        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, ClassifierSystemPrompt),
                new(ChatRole.User, prompt)
            };

            var raw = (await _chatClient.GetResponseAsync(messages)).Text ?? string.Empty;
            var response = StripThinkBlocks(raw);

            var start = response.IndexOf('{');
            var end = response.LastIndexOf('}');
            if (start < 0 || end <= start) return new ClassificationResult(false, "");

            using var doc = JsonDocument.Parse(response[start..(end + 1)]);
            var type = doc.RootElement.GetProperty("type").GetString();

            if (type == "agentic")
            {
                var title = doc.RootElement.TryGetProperty("title", out var t)
                    ? t.GetString() ?? Truncate(prompt, 60)
                    : Truncate(prompt, 60);
                return new ClassificationResult(true, title);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Classification failed — defaulting to direct response");
        }

        return new ClassificationResult(false, "");
    }

    public async Task<string> GenerateTitleAsync(string firstMessage)
    {
        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, TitleSystemPrompt),
                new(ChatRole.User, $"Suggest a title for a conversation that starts with this message:\n\n{firstMessage}")
            };

            var raw = (await _chatClient.GetResponseAsync(messages)).Text ?? string.Empty;
            return CleanTitle(raw);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Title generation failed");
            return string.Empty;
        }
    }

    private static string StripThinkBlocks(string raw) =>
        ThinkBlockRegex.Replace(raw, "").Trim();

    private static string CleanTitle(string raw)
    {
        var stripped = StripThinkBlocks(raw);
        // Some models prefix with "Title:" or wrap in markdown — strip those.
        if (stripped.StartsWith("Title:", StringComparison.OrdinalIgnoreCase))
            stripped = stripped[6..].TrimStart();
        var title = stripped.Trim().Trim('"', '\'', '*', '`').TrimEnd('.', '!', '?');
        return Truncate(title, 50);
    }

    private static string Truncate(string s, int max) =>
        s.Length > max ? s[..max] : s;
}
