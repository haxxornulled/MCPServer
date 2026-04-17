

namespace McpServer.Application.Web
{
    public class FetchedPageResult
    {
        public string? Url { get; set; }
        public string? Content { get; set; }
        public string? ContentType { get; set; }
        public DateTimeOffset FetchedAt { get; set; }
        // Add more metadata as needed
    }
}
