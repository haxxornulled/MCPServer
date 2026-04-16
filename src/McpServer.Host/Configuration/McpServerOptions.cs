namespace McpServer.Host.Configuration;

public sealed class McpServerOptions
{
    public const string SectionName = "McpServer";

    public WorkspaceOptions Workspace { get; init; } = new();
    public WebAccessOptions WebAccess { get; init; } = new();
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
