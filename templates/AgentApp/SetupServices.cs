using AgentApp.Agent;
using Microsoft.Extensions.AI;

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

        builder.Services.AddSingleton<AgentRunner>();

        return builder.Build();
    }
}
