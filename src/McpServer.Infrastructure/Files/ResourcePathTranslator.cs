using LanguageExt;
using LanguageExt.Common;
using McpServer.Application.Abstractions.Files;

namespace McpServer.Infrastructure.Files;

public sealed class ResourcePathTranslator : IResourcePathTranslator
{
    private readonly string _workspaceRoot = Path.GetFullPath(".");

    public ResourcePathTranslator(string workspaceRoot)
    {
        _workspaceRoot = TrimTrailingSeparators(Path.GetFullPath(workspaceRoot));
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

        if (trimmed.Equals(workspaceSegment, StringComparison.OrdinalIgnoreCase))
        {
            Fin<string> success = _workspaceRoot;
            return success;
        }

        if (trimmed.StartsWith(workspaceSegment + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            var relativePath = trimmed[(workspaceSegment.Length + 1)..];
            Fin<string> success = Path.GetFullPath(Path.Combine(_workspaceRoot, relativePath));
            return success;
        }

        return Error.New($"Resource URI must be rooted under /workspace: {parsed}");
    }

    private static string TrimTrailingSeparators(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
