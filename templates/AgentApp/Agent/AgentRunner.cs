using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgentApp.Agent;

/// <summary>
/// Runs a multi-turn conversation loop with the agent.
/// Inject additional services here as your agent grows (tools, memory, etc.).
/// </summary>
public class AgentRunner(IChatClient chatClient, ILogger<AgentRunner> logger)
{
    private readonly AIAgent _agent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
    {
        Name = "AgentApp",
        Instructions = "You are a helpful assistant. Answer clearly and concisely."
    });

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // A thread keeps conversation history across multiple turns.
        // Create a new thread per session, or persist and reload one for continuity.
        var thread = _agent.GetNewThread();

        Console.WriteLine("Agent ready. Type a message and press Enter. Leave blank to exit.");
        Console.WriteLine();

        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write("> ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                break;

            try
            {
                var response = await _agent.RunAsync(input, thread, cancellationToken);
                Console.WriteLine($"\nAgent: {response.Text}\n");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error running agent turn");
                Console.WriteLine($"\n[Error: {ex.Message}]\n");
            }
        }
    }
}
