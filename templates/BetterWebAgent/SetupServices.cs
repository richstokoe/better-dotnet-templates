using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using BetterWebAgent.Agents;
using BetterWebAgent.Chats;
using BetterWebAgent.SlashCommands;
using Microsoft.Extensions.AI;
using OpenAI;

namespace BetterWebAgent;

public static class SetupServices
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddControllersWithViews()
            .AddFeatureSliceViewEngine();

        builder.Services.AddSignalR();

        // Some sensible JSON serialization defaults
        builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(opts =>
        {
            opts.SerializerOptions.Converters.Clear();
            opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        // TODO: Configure your IChatClient. The template defaults to a local LM Studio
        // instance so the project runs out of the box with no API keys. Switch to one of
        // the commented blocks below to use Azure OpenAI or OpenAI instead.
        //
        // Azure OpenAI:
        //   IChatClient chatClient = new Azure.AI.OpenAI.AzureOpenAIClient(
        //       new Uri(builder.Configuration["AzureOpenAI:Endpoint"]!),
        //       new ApiKeyCredential(builder.Configuration["AzureOpenAI:ApiKey"]!))
        //       .GetChatClient(builder.Configuration["AzureOpenAI:DeploymentName"]!)
        //       .AsIChatClient();
        //
        // OpenAI:
        //   IChatClient chatClient = new OpenAIClient(
        //       new ApiKeyCredential(builder.Configuration["OpenAI:ApiKey"]!))
        //       .GetChatClient("gpt-4o-mini")
        //       .AsIChatClient();
        //
        // LM Studio (default — no key required):
        IChatClient chatClient = new OpenAIClient(
            new ApiKeyCredential("no-key-required"),
            new OpenAIClientOptions { Endpoint = new Uri("http://localhost:1234/v1") })
            .GetChatClient("google/gemma-4-26b-a4b")
            .AsIChatClient();

        builder.Services.AddSingleton(chatClient);

        // ToolManager scans all loaded assemblies for [AgentTool]-marked methods
        // and exposes them to the AIAgent. Add new tools by writing static methods
        // annotated with [AgentTool] anywhere in this project (or any referenced one).
        builder.Services.AddSingleton<RichStokoe.AgentTools.ToolManager>();

        // Repositories — swap these registrations for database-backed implementations
        // when you outgrow the in-memory defaults.
        builder.Services.AddSingleton<IChatRepository, InMemoryChatRepository>();
        builder.Services.AddSingleton<IAgentTaskRepository, InMemoryAgentTaskRepository>();

        builder.Services.AddSingleton<WebAgentFactory>();
        builder.Services.AddSingleton<AgentTaskRunner>();

        // Slash commands — register additional ISlashCommand implementations here.
        builder.Services.AddSingleton<ISlashCommand, ClearCommand>();
        builder.Services.AddSingleton<ISlashCommand, NewCommand>();
        builder.Services.AddSingleton<ISlashCommand, HelpCommand>();
        builder.Services.AddSingleton<SlashCommandRegistry>();

        return builder.Build();
    }

    private static IMvcBuilder AddFeatureSliceViewEngine(this IMvcBuilder builder)
    {
        builder.AddRazorOptions(options =>
        {
            options.ViewLocationFormats.Clear();
            options.AreaViewLocationFormats.Clear();

            options.ViewLocationFormats.Add("/{1}/Views/{0}.cshtml");
            options.ViewLocationFormats.Add("/{1}/{0}.cshtml");
            options.ViewLocationFormats.Add("/Shared/Views/{0}.cshtml");
            options.ViewLocationFormats.Add("/Views/{1}/{0}.cshtml");
            options.ViewLocationFormats.Add("/Views/Shared/{0}.cshtml");

            options.AreaViewLocationFormats.Add("/Areas/{2}/{1}/Views/{0}.cshtml");
            options.AreaViewLocationFormats.Add("/Areas/{2}/Shared/Views/{0}.cshtml");
            options.AreaViewLocationFormats.Add("/Shared/Views/{0}.cshtml");
            options.AreaViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}.cshtml");
            options.AreaViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}.cshtml");
            options.AreaViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
        });

        return builder;
    }
}
