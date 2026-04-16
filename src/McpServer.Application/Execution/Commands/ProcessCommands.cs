namespace McpServer.Application.Execution.Commands;

public sealed record RunProcessCommand(
    string Command,
    IReadOnlyList<string> Arguments,
    string? WorkingDirectory,
    int TimeoutSeconds,
    int MaxOutputChars);