using McpServer.Application.Files;

namespace McpServer.Application.Files.Results
{
    public record DirectoryListingResult(string Path, IReadOnlyList<DirectoryEntry> Entries);
}
