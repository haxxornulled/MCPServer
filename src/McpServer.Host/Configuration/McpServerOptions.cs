namespace McpServer.Host.Configuration;

public sealed class McpServerOptions
{
    public const string SectionName = "McpServer";

    public WorkspaceOptions Workspace { get; init; } = new();
    public WebAccessOptions WebAccess { get; init; } = new();
    public SshOptions Ssh { get; init; } = new();
}

public sealed class WorkspaceOptions
{
    public string RootPath { get; init; } = "./workspace";
}

public sealed class WebAccessOptions
{
    public bool Enabled { get; init; }
    public string[] AllowedHosts { get; init; } = [];
}

public sealed class SshOptions
{
    public bool Enabled { get; init; }
    public SshProfileOptions[] Profiles { get; init; } = [];
}

public sealed class SshProfileOptions
{
    public string Name { get; init; } = string.Empty;
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 22;
    public string Username { get; init; } = string.Empty;
    public string? PasswordEnvironmentVariable { get; init; }
    public string? PrivateKeyPath { get; init; }
    public string? PrivateKeyPassphraseEnvironmentVariable { get; init; }
    public string? WorkingDirectory { get; init; }
    public string? HostKeySha256 { get; init; }
    public bool AcceptUnknownHostKey { get; init; }
}
