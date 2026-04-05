using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;

namespace AgentApp;

public static class SetupServices
{
    public static IHost ConfigureServices(this HostApplicationBuilder builder)
    {
        // TODO: Configure your IChatClient and register it with DI.
        //
        // Azure OpenAI:
        //   IChatClient chatClient = new AzureOpenAIClient(
        //       new Uri("https://your-resource.openai.azure.com/"),
        //       new AzureKeyCredential(builder.Configuration["AzureOpenAI:ApiKey"]!))
        //       .GetChatClient("your-deployment-name")
        //       .AsIChatClient();
        //   builder.Services.AddSingleton(chatClient);
        //
        // OpenAI:
        //   IChatClient chatClient = new OpenAIClient(
        //       new ApiKeyCredential(builder.Configuration["OpenAI:ApiKey"]!))
        //       .GetChatClient("gpt-4o-mini")
        //       .AsIChatClient();
        //   builder.Services.AddSingleton(chatClient);
        //
        // LM Studio:
        IChatClient chatClient = new OpenAIClient(
            new ApiKeyCredential("no-key-required"),
            new OpenAIClientOptions { Endpoint = new Uri("http://localhost:1234/v1") })
        .GetChatClient("google/gemma-4-26b-a4b")
        .AsIChatClient();

        builder.Services.AddSingleton(chatClient);
        builder.Services.AddSingleton<RichStokoe.AgentTools.ToolManager>();
        builder.Services.AddScoped<Agent.AgentRunner>();

        return builder.Build();
    }
}
