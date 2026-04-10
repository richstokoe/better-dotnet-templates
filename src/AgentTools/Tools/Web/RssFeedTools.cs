using System.ComponentModel;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Linq;

namespace RichStokoe.AgentTools.Web;

public static class RssFeedTools
{
    private static readonly HttpClient _httpClient = new();

    static RssFeedTools()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "RichStokoe.AgentTools/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    [AgentTool]
    [Description("Reads articles from an RSS or Atom feed. Returns the latest articles with titles, descriptions, and links.")]
    public static async Task<string> Read_Rss_Feed(
        [Description("The URL of the RSS or Atom feed to read.")] string feedUrl,
        [Description("Maximum number of articles to return (default: 5, max: 20).")] int maxArticles = 5)
    {
        try
        {
            maxArticles = Math.Clamp(maxArticles, 1, 20);

            using var response = await _httpClient.GetAsync(feedUrl);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var xmlReader = XmlReader.Create(stream);

            var feed = SyndicationFeed.Load(xmlReader);

            if (feed == null)
            {
                return $"Unable to parse feed from: {feedUrl}";
            }

            var articles = feed.Items.Take(maxArticles).ToList();

            if (articles.Count == 0)
            {
                return $"No articles found in feed: {feed.Title?.Text ?? feedUrl}";
            }

            var output = $"Feed: {feed.Title?.Text ?? "Unknown"}\n";
            if (!string.IsNullOrWhiteSpace(feed.Description?.Text))
            {
                output += $"Description: {feed.Description.Text}\n";
            }
            output += $"\nLatest {articles.Count} articles:\n\n";

            for (int i = 0; i < articles.Count; i++)
            {
                var item = articles[i];
                var title = item.Title?.Text ?? "No title";
                var link = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "No link";
                var published = item.PublishDate != DateTimeOffset.MinValue
                    ? item.PublishDate.ToString("yyyy-MM-dd HH:mm")
                    : "Unknown date";

                var summary = GetArticleSummary(item);

                output += $"{i + 1}. {title}\n";
                output += $"   Published: {published}\n";
                output += $"   URL: {link}\n";
                if (!string.IsNullOrWhiteSpace(summary))
                {
                    output += $"   Summary: {summary}\n";
                }
                output += "\n";
            }

            return output.TrimEnd();
        }
        catch (HttpRequestException ex)
        {
            return $"Error fetching feed: {ex.Message}";
        }
        catch (XmlException ex)
        {
            return $"Error parsing feed XML: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error reading RSS feed: {ex.Message}";
        }
    }

    [AgentTool]
    [Description("Gets the latest news from popular sources. Supported sources: bbc, cnn, reuters, techcrunch, hackernews, guardian, nytimes, reddit.")]
    public static async Task<string> Get_Latest_News(
        [Description("The news source to fetch from (e.g., 'bbc', 'hackernews', 'techcrunch').")] string source,
        [Description("Maximum number of articles to return (default: 5, max: 15).")] int maxArticles = 5)
    {
        if (!FeedUrls.TryGetValue(source, out var feedInfo))
        {
            var availableSources = string.Join(", ", FeedUrls.Keys.OrderBy(k => k));
            return $"Unknown news source: '{source}'. Available sources: {availableSources}";
        }

        return await Read_Rss_Feed(feedInfo.Url, maxArticles);
    }

    [AgentTool]
    [Description("Search for RSS feeds related to a topic. Returns a list of potential RSS feed URLs.")]
    public static async Task<string> Find_Rss_Feeds(
        [Description("The topic or website to find RSS feeds for.")] string query)
    {
        try
        {
            // Use Feedly's feed search API
            var encodedQuery = Uri.EscapeDataString(query);
            var searchUrl = $"https://cloud.feedly.com/v3/search/feeds?query={encodedQuery}&count=10";

            var response = await _httpClient.GetStringAsync(searchUrl);
            var json = System.Text.Json.JsonDocument.Parse(response);

            var results = json.RootElement.GetProperty("results");
            var feeds = new List<string>();

            foreach (var result in results.EnumerateArray().Take(10))
            {
                var title = result.TryGetProperty("title", out var t) ? t.GetString() : "Unknown";
                var feedId = result.GetProperty("feedId").GetString();
                var feedUrl = feedId?.Replace("feed/", "") ?? "";
                var subscribers = result.TryGetProperty("subscribers", out var s) ? s.GetInt32() : 0;

                if (!string.IsNullOrWhiteSpace(feedUrl))
                {
                    feeds.Add($"• {title}\n  URL: {feedUrl}\n  Subscribers: {subscribers:N0}");
                }
            }

            if (feeds.Count == 0)
            {
                return $"No RSS feeds found for: {query}";
            }

            return $"RSS feeds related to '{query}':\n\n{string.Join("\n\n", feeds)}";
        }
        catch (Exception ex)
        {
            return $"Error searching for RSS feeds: {ex.Message}";
        }
    }

    private static readonly Dictionary<string, (string Url, string Name)> FeedUrls =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["bbc"] = ("https://feeds.bbci.co.uk/news/rss.xml", "BBC News"),
            ["cnn"] = ("http://rss.cnn.com/rss/edition.rss", "CNN"),
            ["reuters"] = ("https://www.reutersagency.com/feed/?best-topics=business-finance&post_type=best", "Reuters"),
            ["techcrunch"] = ("https://techcrunch.com/feed/", "TechCrunch"),
            ["hackernews"] = ("https://hnrss.org/frontpage", "Hacker News"),
            ["guardian"] = ("https://www.theguardian.com/world/rss", "The Guardian"),
            ["nytimes"] = ("https://rss.nytimes.com/services/xml/rss/nyt/HomePage.xml", "New York Times"),
            ["reddit"] = ("https://www.reddit.com/.rss", "Reddit Front Page"),
            ["ars"] = ("https://feeds.arstechnica.com/arstechnica/index", "Ars Technica"),
            ["verge"] = ("https://www.theverge.com/rss/index.xml", "The Verge"),
        };

    /// <summary>
    /// Get structured news articles from a source. 
    /// </summary>
    public static async Task<List<NewsArticle>> GetNewsArticlesAsync(string source, int maxArticles = 5)
    {
        if (!FeedUrls.TryGetValue(source, out var feedInfo))
        {
            return [];
        }

        try
        {
            maxArticles = Math.Clamp(maxArticles, 1, 20);

            using var response = await _httpClient.GetAsync(feedInfo.Url);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var xmlReader = XmlReader.Create(stream);

            var feed = SyndicationFeed.Load(xmlReader);
            if (feed == null) return [];

            // Extract channel/feed image (the source logo)
            var sourceImageUrl = feed.ImageUrl?.ToString();

            var articles = new List<NewsArticle>();

            foreach (var item in feed.Items.Take(maxArticles))
            {
                var title = item.Title?.Text ?? "No title";
                var link = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "";
                var published = item.PublishDate != DateTimeOffset.MinValue
                    ? item.PublishDate.DateTime
                    : DateTime.Now;
                var summary = GetArticleSummary(item);
                var imageUrl = ExtractImageUrl(item);

                articles.Add(new NewsArticle(
                    Title: title,
                    Url: link,
                    ImageUrl: imageUrl,
                    Summary: summary,
                    Source: feedInfo.Name,
                    SourceImageUrl: sourceImageUrl,
                    PublishedAt: published
                ));
            }

            return articles;
        }
        catch
        {
            return [];
        }
    }

    private static string? ExtractImageUrl(SyndicationItem item)
    {
        // Try media:thumbnail or media:content
        var mediaNamespace = "http://search.yahoo.com/mrss/";

        foreach (var ext in item.ElementExtensions)
        {
            try
            {
                var element = ext.GetObject<XElement>();

                // Check for media:thumbnail
                if (element.Name.LocalName == "thumbnail" &&
                    element.Name.NamespaceName == mediaNamespace)
                {
                    var url = element.Attribute("url")?.Value;
                    if (!string.IsNullOrWhiteSpace(url)) return url;
                }

                // Check for media:content with medium="image"
                if (element.Name.LocalName == "content" &&
                    element.Name.NamespaceName == mediaNamespace)
                {
                    var medium = element.Attribute("medium")?.Value;
                    if (medium == "image")
                    {
                        var url = element.Attribute("url")?.Value;
                        if (!string.IsNullOrWhiteSpace(url)) return url;
                    }
                }

                // Check for media:group containing media:content
                if (element.Name.LocalName == "group" &&
                    element.Name.NamespaceName == mediaNamespace)
                {
                    var content = element.Descendants()
                        .FirstOrDefault(d => d.Name.LocalName == "content" || d.Name.LocalName == "thumbnail");
                    var url = content?.Attribute("url")?.Value;
                    if (!string.IsNullOrWhiteSpace(url)) return url;
                }
            }
            catch
            {
                // Skip malformed extensions
            }
        }

        // Try enclosure with image type
        foreach (var link in item.Links)
        {
            if (link.RelationshipType == "enclosure" &&
                link.MediaType?.StartsWith("image/") == true)
            {
                return link.Uri?.ToString();
            }
        }

        // Try to extract image from summary/content HTML
        var html = item.Summary?.Text ?? (item.Content as TextSyndicationContent)?.Text;
        if (!string.IsNullOrWhiteSpace(html))
        {
            var imgMatch = System.Text.RegularExpressions.Regex.Match(
                html,
                @"<img[^>]+src=[""']([^""']+)[""']",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (imgMatch.Success)
            {
                return imgMatch.Groups[1].Value;
            }
        }

        return null;
    }

    private static string GetArticleSummary(SyndicationItem item)
    {
        var summary = item.Summary?.Text;

        if (string.IsNullOrWhiteSpace(summary) && item.Content is TextSyndicationContent textContent)
        {
            summary = textContent.Text;
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            return "";
        }

        // Strip HTML tags
        summary = System.Text.RegularExpressions.Regex.Replace(summary, "<[^>]*>", "");
        summary = System.Net.WebUtility.HtmlDecode(summary);

        // Truncate if too long
        const int maxLength = 200;
        if (summary.Length > maxLength)
        {
            summary = summary.Substring(0, maxLength).TrimEnd() + "...";
        }

        return summary.Trim();
    }
}
