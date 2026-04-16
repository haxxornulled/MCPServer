namespace McpServer.Application.Files.Results;

public sealed record FileTextResult(string Path, string Content, string Encoding, long Length);

public sealed record DirectoryEntryResult(
    string Name,
    string FullPath,
    bool IsDirectory,
    long? Size,
    DateTimeOffset LastWriteTimeUtc);

public sealed record DirectoryListingResult(
    string Path,
    IReadOnlyList<DirectoryEntryResult> Entries);

public sealed record FileMetadataResult(
    string Path,
    bool Exists,
    bool IsDirectory,
    long? Size,
    DateTimeOffset? CreationTimeUtc,
    DateTimeOffset? LastWriteTimeUtc,
    string? Attributes);
