using LanguageExt;
using LanguageExt.Common;
using McpServer.Application.Abstractions.Files;

namespace McpServer.Infrastructure.Files;

public sealed class ResourcePathTranslator : IResourcePathTranslator
{
    private readonly object _sync = new();
    private string _workspaceRoot = Path.GetFullPath(".");
    private string _projectRoot = Path.GetFullPath(".");

    public ResourcePathTranslator(string workspaceRoot)
    {
        var normalized = TrimTrailingSeparators(Path.GetFullPath(workspaceRoot));
        _workspaceRoot = normalized;
        _projectRoot = normalized;
    }

    public void SetWorkspaceRoot(string workspaceRoot)
    {
        lock (_sync)
        {
            var normalized = TrimTrailingSeparators(Path.GetFullPath(workspaceRoot));
            _workspaceRoot = normalized;
            _projectRoot = normalized;
        }
    }

    public void SetProjectRoot(string projectRoot)
    {
        lock (_sync)
        {
            _projectRoot = TrimTrailingSeparators(Path.GetFullPath(projectRoot));
        }
    }

    public Fin<string> TryTranslateToLocalPath(string uri)
    {
        try
        {
            if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
            {
                return Error.New($"Invalid URI: {uri}");
            }

            return parsed.Scheme switch
            {
                "file" or "dir" or "filemeta" => TranslateWorkspaceUri(parsed),
                _ => Error.New($"Unsupported URI scheme: {parsed.Scheme}")
            };
        }
        catch (Exception ex)
        {
            return Error.New(ex.Message);
        }
    }

    private Fin<string> TranslateWorkspaceUri(Uri parsed)
    {
        var path = parsed.LocalPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var trimmed = path.TrimStart(Path.DirectorySeparatorChar);

        const string workspaceSegment = "workspace";
        const string projectSegment = "project";
        var workspaceRoot = _workspaceRoot;
        var projectRoot = _projectRoot;

        if (trimmed.Equals(workspaceSegment, StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("mcpserver-filesystem", StringComparison.OrdinalIgnoreCase))
        {
            Fin<string> success = workspaceRoot;
            return success;
        }

        if (trimmed.StartsWith(workspaceSegment + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("mcpserver-filesystem" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            var segment = trimmed.StartsWith(workspaceSegment + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                ? workspaceSegment
                : "mcpserver-filesystem";
            var relativePath = trimmed[(segment.Length + 1)..];
            Fin<string> success = Path.GetFullPath(Path.Combine(workspaceRoot, relativePath));
            return success;
        }

        if (trimmed.Equals(projectSegment, StringComparison.OrdinalIgnoreCase))
        {
            Fin<string> success = projectRoot;
            return success;
        }

        if (trimmed.StartsWith(projectSegment + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            var relativePath = trimmed[(projectSegment.Length + 1)..];
            Fin<string> success = Path.GetFullPath(Path.Combine(projectRoot, relativePath));
            return success;
        }

        return Error.New($"Resource URI must be rooted under /workspace or /project: {parsed}");
    }

    private static string TrimTrailingSeparators(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
