using LanguageExt;

namespace McpServer.Application.Abstractions.Mcp;

public interface IResourceHandler
{
    string UriScheme { get; }
    string Name { get; }
    string Description { get; }
    ValueTask<Fin<ReadResourceResult>> ReadAsync(string uri, CancellationToken ct);
    ResourceDescriptor Describe();
}

public sealed record ReadResourceResult(IReadOnlyList<ResourceContent> Contents);

public sealed record ResourceContent(
    string Uri,
    string MimeType,
    string? Text = null,
    string? BlobBase64 = null);

public sealed record ResourceDescriptor(
    string Name,
    string? Title,
    string Uri,
    string? Description,
    string? MimeType = null,
    long? Size = null);
