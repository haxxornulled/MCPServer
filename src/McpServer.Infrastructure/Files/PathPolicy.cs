using LanguageExt;
using LanguageExt.Common;
using McpServer.Application.Abstractions.Files;

namespace McpServer.Infrastructure.Files;

public sealed class PathPolicy : IPathPolicy
{
    private static readonly string[] WorkspaceAliases = ["workspace", "mcpserver-filesystem"];
    private static readonly string[] ProjectAliases = ["project"];
    private readonly object _sync = new();
    private string[] _roots;
    private string[] _rootPrefixes;
    private string _workspaceRoot;
    private string _projectRoot;

    public PathPolicy(IEnumerable<string> allowedRoots)
    {
        (_roots, _rootPrefixes) = BuildRootCache(allowedRoots);
        _workspaceRoot = _roots.FirstOrDefault() ?? string.Empty;
        _projectRoot = _workspaceRoot;
    }

    public Fin<string> NormalizeAndValidateReadPath(string rawPath) => Normalize(rawPath);
    public Fin<string> NormalizeAndValidateWritePath(string rawPath) => Normalize(rawPath);

    public void SetAllowedRoots(IEnumerable<string> allowedRoots)
    {
        var cache = BuildRootCache(allowedRoots);
        lock (_sync)
        {
            _roots = cache.Roots;
            _rootPrefixes = cache.RootPrefixes;
            _workspaceRoot = _roots.FirstOrDefault() ?? string.Empty;
            _projectRoot = _workspaceRoot;
        }
    }

    public void SetProjectRoot(string projectRoot)
    {
        var normalized = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        lock (_sync)
        {
            if (_roots.Length == 0)
            {
                _projectRoot = normalized;
                return;
            }

            if (!IsUnderAllowedRoot(normalized, _roots, _rootPrefixes))
            {
                throw new InvalidOperationException($"Project root '{projectRoot}' is outside the allowed roots.");
            }

            _projectRoot = normalized;
        }
    }

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

            var roots = _roots;
            var rootPrefixes = _rootPrefixes;
            var full = ResolvePath(rawPath);

            var allowed = IsUnderAllowedRoot(full, roots, rootPrefixes);

            if (allowed)
            {
                Fin<string> success = full;
                return success;
            }

            return Error.New($"Path '{rawPath}' is outside allowed roots. Allowed roots: {string.Join(", ", _roots)}");
        }
        catch (Exception ex)
        {
            return Error.New(ex.Message);
        }
    }

    private string ResolvePath(string rawPath)
    {
        var workspaceRoot = _workspaceRoot;
        var projectRoot = _projectRoot;
        var trimmed = rawPath.Trim();

        if (TryResolveRelativePath(trimmed, WorkspaceAliases, out var relativePath))
        {
            return TrimTrailingSeparators(Path.GetFullPath(Path.Combine(workspaceRoot, relativePath)));
        }

        if (TryResolveRelativePath(trimmed, ProjectAliases, out relativePath))
        {
            return TrimTrailingSeparators(Path.GetFullPath(Path.Combine(projectRoot, relativePath)));
        }

        if (Path.IsPathRooted(trimmed))
        {
            return TrimTrailingSeparators(Path.GetFullPath(trimmed));
        }

        return TrimTrailingSeparators(Path.GetFullPath(Path.Combine(projectRoot, trimmed)));
    }

    private static bool TryResolveRelativePath(string rawPath, IReadOnlyList<string> aliases, out string relativePath)
    {
        var normalized = NormalizeDirectorySeparators(rawPath);
        var span = normalized.AsSpan();

        while (span.StartsWith($".{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            span = span[2..];
        }

        while (span.Length > 0 && span[0] == Path.DirectorySeparatorChar)
        {
            span = span[1..];
        }

        foreach (var alias in aliases)
        {
            if (span.Equals(alias.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                relativePath = string.Empty;
                return true;
            }

            if (span.StartsWith(alias.AsSpan(), StringComparison.OrdinalIgnoreCase) &&
                span.Length > alias.Length &&
                span[alias.Length] == Path.DirectorySeparatorChar)
            {
                relativePath = span[(alias.Length + 1)..].ToString();
                return true;
            }
        }

        relativePath = string.Empty;
        return false;
    }

    private static string TrimTrailingSeparators(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static bool IsUnderAllowedRoot(string fullPath, IReadOnlyList<string> roots, IReadOnlyList<string> rootPrefixes)
    {
        for (var i = 0; i < roots.Count; i++)
        {
            if (fullPath.Equals(roots[i], PathComparison.Comparison) ||
                fullPath.StartsWith(rootPrefixes[i], PathComparison.Comparison))
            {
                return true;
            }
        }

        return false;
    }

    private static (string[] Roots, string[] RootPrefixes) BuildRootCache(IEnumerable<string> allowedRoots)
    {
        var roots = allowedRoots
            .Select(Path.GetFullPath)
            .Select(static p => TrimTrailingSeparators(p))
            .ToArray();

        var rootPrefixes = roots
            .Select(root => root + Path.DirectorySeparatorChar)
            .ToArray();

        return (roots, rootPrefixes);
    }

    private static string NormalizeDirectorySeparators(string rawPath)
    {
        if (rawPath.IndexOf(Path.AltDirectorySeparatorChar) < 0)
        {
            return rawPath;
        }

        return rawPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }
}
