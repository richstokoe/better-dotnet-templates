# RichStokoe.AgentTools

A collection of ready-made tools for AI agents built with [Microsoft Agent Framework (MAF)](https://learn.microsoft.com/en-us/agent-framework/overview/). Drop them into any MAF agent to give it real-world capabilities without writing boilerplate.

## Installation

```
dotnet add package RichStokoe.AgentTools
```

## Usage

Register `ToolManager` in your DI container and pass its tools to your agent:

```csharp
// SetupServices.cs
builder.Services.AddSingleton<ToolManager>();

// Agent/AgentRunner.cs
public class AgentRunner(IChatClient chatClient, ToolManager toolManager)
{
    private readonly AIAgent _agent = chatClient.AsAIAgent(
        name: "MyAgent",
        instructions: "You are a helpful assistant.",
        tools: toolManager.GetTools()
    );
}
```

`ToolManager` discovers tools at runtime by scanning all assemblies loaded into the current `AssemblyLoadContext`. Any `public static` method decorated with `[AgentTool]` (and `[Description]`) is registered as an `AIFunction`.

### Filtering tools

`GetTools` accepts optional filters so you can pass only a relevant subset to an agent:

```csharp
// All tools whose name contains "Weather" (case-insensitive)
toolManager.GetTools(namePattern: "*Weather*")

// All read-only tools
toolManager.GetTools(typeFilter: AgentToolTypes.Read)

// Read tools whose name starts with "Get"
toolManager.GetTools(namePattern: "Get_*", typeFilter: AgentToolTypes.Read)
```

`namePattern` supports `*` (any sequence of characters) and `?` (any single character) wildcards. The pattern is matched against the tool's `Name` if one was set on the attribute, otherwise against the method name.

`typeFilter` is a flags mask — pass `AgentToolTypes.Read | AgentToolTypes.Write` to include all classified tools. Tools marked `AgentToolTypes.None` (unclassified) are only returned when no type filter is specified.

---

## Tools

### Utils

#### DateTimeTools

Gives the agent awareness of the current local date and time. Instructs the model not to cache these values, so repeated calls always return fresh results.

| Tool | Description |
|---|---|
| `Get_Current_Time` | Returns the current local time |
| `Get_Current_Date` | Returns the current local date |

#### WeatherTools

Fetches live weather data via [wttr.in](https://wttr.in). No API key required.

| Tool | Description |
|---|---|
| `Get_Current_Weather_For_Location` | Returns temperature (°C/°F), conditions, humidity, wind speed and direction, visibility, and UV index for a city name or lat/long coordinates |

#### LocationTools

Resolves an IP address to a geographic location via [ip-api.com](https://ip-api.com) (free for non-commercial use, no API key required). Pair with `NetworkTools` so the agent can look up the user's own location.

| Tool | Description |
|---|---|
| `Get_Location_From_Ip_Address` | Returns city, region, country, coordinates, timezone, and ISP for a given IP address |

#### NetworkTools

| Tool | Description |
|---|---|
| `Get_Public_Ip_Address` | Returns the machine's current public IP address via [ipify.org](https://api.ipify.org) |

#### MathTools

| Tool | Description |
|---|---|
| `Add_Numbers` | Adds a sequence of decimal numbers and returns the sum |

---

### Web

#### WebSearchTools

Searches the web via DuckDuckGo without an API key.

| Tool | Description |
|---|---|
| `Search_Web` | Returns titles, URLs, and snippets for a search query (1–10 results) |
| `Get_Instant_Answer` | Returns a direct answer, Wikipedia-style abstract, or definition for a factual question via the DuckDuckGo Instant Answer API |

#### RssFeedTools

Reads and searches RSS/Atom feeds. Supports both arbitrary feed URLs and a set of named news sources.

| Tool | Description |
|---|---|
| `Read_Rss_Feed` | Reads up to 20 articles from any RSS or Atom feed URL |
| `Get_Latest_News` | Fetches headlines from a named source (see supported sources below) |
| `Find_Rss_Feeds` | Searches for RSS feeds related to a topic via the Feedly API |

**Supported news sources** for `Get_Latest_News`:

| Key | Source |
|---|---|
| `bbc` | BBC News |
| `cnn` | CNN |
| `reuters` | Reuters |
| `techcrunch` | TechCrunch |
| `hackernews` | Hacker News |
| `guardian` | The Guardian |
| `nytimes` | New York Times |
| `reddit` | Reddit Front Page |
| `ars` | Ars Technica |
| `verge` | The Verge |

---

## Adding Your Own Tools

Decorate any `public static` method with `[AgentTool]` and `[Description]`. The method can live in any assembly that is loaded by the application — `ToolManager` scans all of them.

```csharp
using System.ComponentModel;
using RichStokoe.AgentTools;

public static class MyTools
{
    [AgentTool(Type = AgentToolTypes.Read)]
    [Description("Returns a friendly greeting for the given name.")]
    public static string Greet(
        [Description("The name to greet.")] string name)
        => $"Hello, {name}!";
}
```

`[AgentTool]` has two optional properties:

| Property | Type | Description |
|---|---|---|
| `Name` | `string?` | Override the tool name exposed to the model. Defaults to the method name. |
| `Type` | `AgentToolTypes` | Classify the tool as `Read`, `Write`, or `Read \| Write`. Defaults to `None`. |

No registration required — just ensure the assembly is loaded and restart.

---

## External Services

All tools that call external services use free, no-registration APIs:

| Service | Used by | Terms |
|---|---|---|
| [wttr.in](https://wttr.in) | WeatherTools | Free, no key |
| [ip-api.com](https://ip-api.com) | LocationTools | Free for non-commercial use |
| [ipify.org](https://api.ipify.org) | NetworkTools | Free, no key |
| [DuckDuckGo](https://duckduckgo.com) | WebSearchTools | Free, no key |
| [Feedly](https://cloud.feedly.com) | RssFeedTools (Find) | Free tier |
| RSS/Atom feeds | RssFeedTools | Per-source terms |

## License

[MIT](../../LICENSE) — provided as-is, without warranty of any kind. See the [LICENSE](../../LICENSE) file for the full terms.
