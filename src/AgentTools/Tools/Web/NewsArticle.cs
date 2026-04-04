namespace RichStokoe.AgentTools.Web;

public record NewsArticle(
    string Title,
    string Url,
    string? ImageUrl,
    string? Summary,
    string Source,
    string? SourceImageUrl,
    DateTime PublishedAt
);
