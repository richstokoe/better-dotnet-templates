using System.ComponentModel;
using System.Text.Json;
using System.Web;

namespace RichStokoe.AgentTools.Web;

public static class WebSearchTools
{
    private static readonly HttpClient _httpClient = new();

    static WebSearchTools()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "RichStokoe.AgentTools/1.0");
    }

    [AgentTool]
    [Description("Search the web for information. Returns relevant search results including titles, URLs, and snippets.")]
    public static async Task<string> Search_Web(
        [Description("The search query to find information about.")] string query,
        [Description("Maximum number of results to return (default: 5, max: 10).")] int maxResults = 5)
    {
        try
        {
            maxResults = Math.Clamp(maxResults, 1, 10);
            var encodedQuery = HttpUtility.UrlEncode(query);

            // Use DuckDuckGo HTML search and parse results
            var url = $"https://html.duckduckgo.com/html/?q={encodedQuery}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", "text/html");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();

            var results = ParseDuckDuckGoResults(html, maxResults);

            if (results.Count == 0)
            {
                return $"No search results found for: {query}";
            }

            var output = $"Search results for: {query}\n\n";
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                output += $"{i + 1}. {result.Title}\n";
                output += $"   URL: {result.Url}\n";
                output += $"   {result.Snippet}\n\n";
            }

            return output.TrimEnd();
        }
        catch (Exception ex)
        {
            return $"Error performing web search: {ex.Message}";
        }
    }

    [AgentTool]
    [Description("Get a quick answer or summary for a factual question using DuckDuckGo's Instant Answer API.")]
    public static async Task<string> Get_Instant_Answer(
        [Description("The question or topic to get an instant answer for.")] string query)
    {
        try
        {
            var encodedQuery = HttpUtility.UrlEncode(query);
            var url = $"https://api.duckduckgo.com/?q={encodedQuery}&format=json&no_html=1&skip_disambig=1";

            var response = await _httpClient.GetStringAsync(url);
            var json = JsonDocument.Parse(response);
            var root = json.RootElement;

            // Check for Abstract (Wikipedia-style summary)
            var abstractText = root.GetProperty("AbstractText").GetString();
            var abstractSource = root.GetProperty("AbstractSource").GetString();
            var abstractUrl = root.GetProperty("AbstractURL").GetString();

            if (!string.IsNullOrWhiteSpace(abstractText))
            {
                var result = $"Answer: {abstractText}\n";
                if (!string.IsNullOrWhiteSpace(abstractSource))
                {
                    result += $"Source: {abstractSource}";
                    if (!string.IsNullOrWhiteSpace(abstractUrl))
                    {
                        result += $" ({abstractUrl})";
                    }
                }
                return result;
            }

            // Check for Answer (direct answer)
            var answer = root.GetProperty("Answer").GetString();
            if (!string.IsNullOrWhiteSpace(answer))
            {
                return $"Answer: {answer}";
            }

            // Check for Definition
            var definition = root.GetProperty("Definition").GetString();
            var definitionSource = root.GetProperty("DefinitionSource").GetString();
            if (!string.IsNullOrWhiteSpace(definition))
            {
                var result = $"Definition: {definition}";
                if (!string.IsNullOrWhiteSpace(definitionSource))
                {
                    result += $"\nSource: {definitionSource}";
                }
                return result;
            }

            // Check for Related Topics
            if (root.TryGetProperty("RelatedTopics", out var relatedTopics) &&
                relatedTopics.ValueKind == JsonValueKind.Array &&
                relatedTopics.GetArrayLength() > 0)
            {
                var topics = new List<string>();
                foreach (var topic in relatedTopics.EnumerateArray().Take(5))
                {
                    if (topic.TryGetProperty("Text", out var text) && !string.IsNullOrWhiteSpace(text.GetString()))
                    {
                        topics.Add($"• {text.GetString()}");
                    }
                }
                if (topics.Count > 0)
                {
                    return $"Related information for '{query}':\n{string.Join("\n", topics)}";
                }
            }

            return $"No instant answer available for: {query}. Try using SearchWeb for more comprehensive results.";
        }
        catch (Exception ex)
        {
            return $"Error getting instant answer: {ex.Message}";
        }
    }

    private static List<SearchResult> ParseDuckDuckGoResults(string html, int maxResults)
    {
        var results = new List<SearchResult>();

        // Parse DuckDuckGo HTML results
        // Results are in <a class="result__a"> tags with href and text
        // Snippets are in <a class="result__snippet"> tags

        var resultStartTag = "class=\"result__a\"";
        var snippetStartTag = "class=\"result__snippet\"";

        int position = 0;
        while (results.Count < maxResults)
        {
            // Find the next result link
            int linkStart = html.IndexOf(resultStartTag, position);
            if (linkStart == -1) break;

            // Find href
            int hrefStart = html.LastIndexOf("href=\"", linkStart);
            if (hrefStart == -1 || hrefStart < position - 200)
            {
                position = linkStart + 1;
                continue;
            }

            hrefStart += 6; // Skip 'href="'
            int hrefEnd = html.IndexOf("\"", hrefStart);
            if (hrefEnd == -1)
            {
                position = linkStart + 1;
                continue;
            }

            var href = html.Substring(hrefStart, hrefEnd - hrefStart);

            // Extract actual URL from DuckDuckGo redirect
            href = ExtractUrlFromDdgRedirect(href);

            // Find title (content between > and </a>)
            int titleStart = html.IndexOf(">", linkStart);
            if (titleStart == -1)
            {
                position = linkStart + 1;
                continue;
            }
            titleStart++;

            int titleEnd = html.IndexOf("</a>", titleStart);
            if (titleEnd == -1)
            {
                position = linkStart + 1;
                continue;
            }

            var title = StripHtmlTags(html.Substring(titleStart, titleEnd - titleStart)).Trim();

            // Find snippet
            int snippetStart = html.IndexOf(snippetStartTag, titleEnd);
            string snippet = "";

            if (snippetStart != -1 && snippetStart < titleEnd + 500)
            {
                int snippetContentStart = html.IndexOf(">", snippetStart);
                if (snippetContentStart != -1)
                {
                    snippetContentStart++;
                    int snippetEnd = html.IndexOf("</a>", snippetContentStart);
                    if (snippetEnd != -1)
                    {
                        snippet = StripHtmlTags(html.Substring(snippetContentStart, snippetEnd - snippetContentStart)).Trim();
                    }
                }
                position = snippetStart + 1;
            }
            else
            {
                position = titleEnd + 1;
            }

            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(href))
            {
                results.Add(new SearchResult
                {
                    Title = HttpUtility.HtmlDecode(title),
                    Url = href,
                    Snippet = HttpUtility.HtmlDecode(snippet)
                });
            }
        }

        return results;
    }

    private static string ExtractUrlFromDdgRedirect(string href)
    {
        // DuckDuckGo uses redirect URLs like //duckduckgo.com/l/?uddg=https%3A%2F%2F...
        if (href.Contains("uddg="))
        {
            int uddgStart = href.IndexOf("uddg=") + 5;
            int uddgEnd = href.IndexOf("&", uddgStart);
            if (uddgEnd == -1) uddgEnd = href.Length;

            var encodedUrl = href.Substring(uddgStart, uddgEnd - uddgStart);
            return HttpUtility.UrlDecode(encodedUrl);
        }

        // Handle relative URLs
        if (href.StartsWith("//"))
        {
            return "https:" + href;
        }

        return href;
    }

    private static string StripHtmlTags(string html)
    {
        // Simple HTML tag stripping
        var result = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", "");
        // Normalize whitespace
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ");
        return result.Trim();
    }

    private class SearchResult
    {
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public string Snippet { get; set; } = "";
    }
}
