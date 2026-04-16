namespace McpServer.Application.Ssh.Results;

public sealed record SshCommandResult(
    string Profile,
    string Host,
    int Port,
    string Username,
    string Command,
    string WorkingDirectory,
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut,
    bool OutputTruncated);

public sealed record SshWriteTextResult(
    string Profile,
    string Host,
    int Port,
    string Username,
    string Path,
    long BytesWritten,
    bool Overwritten,
    bool CreatedDirectories,
    string? PermissionsApplied);