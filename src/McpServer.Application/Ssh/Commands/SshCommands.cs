namespace McpServer.Application.Ssh.Commands;

public sealed record ExecuteSshCommand(
    string Profile,
    string Command,
    string? WorkingDirectory,
    int TimeoutSeconds,
    int MaxOutputChars);

public sealed record WriteSshTextCommand(
    string Profile,
    string Path,
    string Content,
    bool Overwrite,
    bool CreateDirectories,
    string Encoding,
    string? Permissions);