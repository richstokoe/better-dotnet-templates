using BetterAgent.Agent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BetterAgent;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateApplicationBuilder(args).ConfigureServices();
        await host.Services.GetRequiredService<AgentRunner>().RunAsync();
    }
}
