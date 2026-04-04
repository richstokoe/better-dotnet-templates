using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgentApp.Agent;

/// <summary>
/// Runs a multi-turn conversation loop with the agent.
/// Inject additional services here as your agent grows (tools, memory, etc.).
/// </summary>
public class AgentRunner(
    IChatClient chatClient,
    ILogger<AgentRunner> logger,
    ToolManager toolManager)
{
    private readonly AIAgent _agent = chatClient.AsAIAgent(
        name: "AgentApp",
        instructions: "You are a helpful assistant. Answer clearly and concisely.",
        tools: toolManager.GetTools()
    );

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // A thread keeps conversation history across multiple turns.
        // Create a new thread per session, or persist and reload one for continuity.
        var session = await _agent.CreateSessionAsync();

        Console.WriteLine("Type a message and press Enter. Leave blank to exit.");
        Console.WriteLine();

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write("> ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                break;

            try
            {
                Console.WriteLine("Thinking...");
                await foreach (var update in _agent.RunStreamingAsync(input, session))
                {
                    Console.Write(update.Text);
                    Console.Out.Flush();
                }
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                var baseException = ex.GetBaseException().Message;
                logger.LogError(baseException);
                Console.WriteLine($"ERROR: {baseException}");
            }
        }
    }
}
