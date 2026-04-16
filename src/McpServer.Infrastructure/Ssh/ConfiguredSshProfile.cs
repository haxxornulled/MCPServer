namespace McpServer.Infrastructure.Ssh;

public sealed record ConfiguredSshProfile(
    string Name,
    string Host,
    int Port,
    string Username,
    string? PasswordEnvironmentVariable,
    string? PrivateKeyPath,
    string? PrivateKeyPassphraseEnvironmentVariable,
    string? WorkingDirectory,
    string? HostKeySha256,
    bool AcceptUnknownHostKey);