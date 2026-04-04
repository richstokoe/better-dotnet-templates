using AgentApp.Agent;

namespace AgentApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateApplicationBuilder(args).ConfigureServices();
        await host.Services.GetRequiredService<AgentRunner>().RunAsync();
    }
}
