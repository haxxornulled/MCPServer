using McpServer.Application.Abstractions.Files;

namespace McpServer.Infrastructure.Files;

public sealed class WorkspaceFileWatcher : IWorkspaceFileWatcher, IDisposable
{
    private readonly object _sync = new();
    private readonly IWorkspaceChangeFeed _changeFeed;
    private FileSystemWatcher? _watcher;

    public WorkspaceFileWatcher(IWorkspaceChangeFeed changeFeed)
    {
        _changeFeed = changeFeed;
    }

    public void SetProjectRoot(string projectRoot)
    {
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            throw new ArgumentException("Project root is required.", nameof(projectRoot));
        }

        var normalized = Path.GetFullPath(projectRoot);
        if (!Directory.Exists(normalized))
        {
            Directory.CreateDirectory(normalized);
        }

        lock (_sync)
        {
            _watcher?.Dispose();

            var watcher = new FileSystemWatcher(normalized)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName
                    | NotifyFilters.DirectoryName
                    | NotifyFilters.LastWrite
                    | NotifyFilters.CreationTime
                    | NotifyFilters.Size
            };

            watcher.Changed += (_, e) => Publish("changed", e.FullPath);
            watcher.Created += (_, e) => Publish("created", e.FullPath);
            watcher.Deleted += (_, e) => Publish("deleted", e.FullPath);
            watcher.Renamed += (_, e) =>
            {
                Publish("renamed", e.FullPath, $"from={e.OldFullPath}");
            };

            _watcher = watcher;
        }
    }

    private void Publish(string operation, string path, string? details = null) =>
        _changeFeed.RecordChange(operation, path, details, source: "watcher");

    public void Dispose()
    {
        lock (_sync)
        {
            _watcher?.Dispose();
            _watcher = null;
        }
    }
}
