using LanguageExt;
using LanguageExt.Common;
using McpServer.Application.Abstractions.Files;

namespace McpServer.Infrastructure.Files;

public sealed class ResourcePathTranslator : IResourcePathTranslator
{
    private readonly object _sync = new();
    private string _workspaceRoot = Path.GetFullPath(".");

    public ResourcePathTranslator(string workspaceRoot)
    {
        _workspaceRoot = TrimTrailingSeparators(Path.GetFullPath(workspaceRoot));
    }

    public void SetWorkspaceRoot(string workspaceRoot)
    {
        lock (_sync)
        {
            _workspaceRoot = TrimTrailingSeparators(Path.GetFullPath(workspaceRoot));
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

        if (trimmed.Equals(workspaceSegment, StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals(projectSegment, StringComparison.OrdinalIgnoreCase))
        {
            Fin<string> success = workspaceRoot;
            return success;
        }

        if (trimmed.StartsWith(workspaceSegment + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith(projectSegment + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            var segment = trimmed.StartsWith(workspaceSegment + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                ? workspaceSegment
                : projectSegment;
            var relativePath = trimmed[(segment.Length + 1)..];
            Fin<string> success = Path.GetFullPath(Path.Combine(workspaceRoot, relativePath));
            return success;
        }

        return Error.New($"Resource URI must be rooted under /workspace: {parsed}");
    }

    private static string TrimTrailingSeparators(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
