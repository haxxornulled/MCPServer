using System.Text;

namespace McpServer.Application.Files.Commands;

public sealed record ReadFileTextCommand(string Path, Encoding? Encoding);
public sealed record WriteFileTextCommand(string Path, string Content, string? EncodingName, bool Overwrite, bool Flush);
public sealed record AppendFileTextCommand(string Path, string Content, string? EncodingName, bool Flush);
public sealed record CreateDirectoryCommand(string Path);
public sealed record ListDirectoryCommand(string Path, string SearchPattern = "*");
public sealed record GetMetadataCommand(string Path);
public sealed record MovePathCommand(string SourcePath, string DestinationPath, bool Overwrite);
public sealed record CopyPathCommand(string SourcePath, string DestinationPath, bool Overwrite, bool Recursive);
public sealed record DeletePathCommand(string Path, bool Recursive);
