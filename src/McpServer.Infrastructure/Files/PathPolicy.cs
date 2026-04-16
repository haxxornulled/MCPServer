using LanguageExt;
using LanguageExt.Common;
using McpServer.Application.Abstractions.Files;

namespace McpServer.Infrastructure.Files;

public sealed class PathPolicy(IEnumerable<string> allowedRoots) : IPathPolicy
{
    private readonly string[] _roots = allowedRoots
        .Select(Path.GetFullPath)
        .Select(static p => TrimTrailingSeparators(p))
        .ToArray();

    private string PrimaryRoot => _roots[0];

    public Fin<string> NormalizeAndValidateReadPath(string rawPath) => Normalize(rawPath);
    public Fin<string> NormalizeAndValidateWritePath(string rawPath) => Normalize(rawPath);

    private Fin<string> Normalize(string rawPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                return Error.New("Path is required");
            }

            if (_roots.Length == 0)
            {
                return Error.New("At least one allowed root is required");
            }

            var full = ResolvePath(rawPath);

            var allowed = _roots.Any(root =>
                full.Equals(root, PathComparison.Comparison) ||
                full.StartsWith(root + Path.DirectorySeparatorChar, PathComparison.Comparison));

            if (allowed)
            {
                Fin<string> success = full;
                return success;
            }

            return Error.New($"Path '{rawPath}' is outside allowed roots");
        }
        catch (Exception ex)
        {
            return Error.New(ex.Message);
        }
    }

    private string ResolvePath(string rawPath)
    {
        var trimmed = rawPath.Trim();

        if (TryResolveWorkspaceRelativePath(trimmed, out var relativePath))
        {
            return TrimTrailingSeparators(Path.GetFullPath(Path.Combine(PrimaryRoot, relativePath)));
        }

        if (Path.IsPathRooted(trimmed))
        {
            return TrimTrailingSeparators(Path.GetFullPath(trimmed));
        }

        return TrimTrailingSeparators(Path.GetFullPath(Path.Combine(PrimaryRoot, trimmed)));
    }

    private static bool TryResolveWorkspaceRelativePath(string rawPath, out string relativePath)
    {
        var normalized = rawPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        while (normalized.StartsWith($".{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        normalized = normalized.TrimStart(Path.DirectorySeparatorChar);

        const string workspaceSegment = "workspace";

        if (normalized.Equals(workspaceSegment, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = string.Empty;
            return true;
        }

        if (normalized.StartsWith(workspaceSegment + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = normalized[(workspaceSegment.Length + 1)..];
            return true;
        }

        relativePath = string.Empty;
        return false;
    }

    private static string TrimTrailingSeparators(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
