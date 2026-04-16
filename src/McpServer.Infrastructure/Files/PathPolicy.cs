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

            var full = TrimTrailingSeparators(Path.GetFullPath(rawPath));

            var allowed = _roots.Any(root =>
                full.Equals(root, PathComparison.Comparison) ||
                full.StartsWith(root + Path.DirectorySeparatorChar, PathComparison.Comparison));

            return allowed ? full : Error.New($"Path '{rawPath}' is outside allowed roots");
        }
        catch (Exception ex)
        {
            return Error.New(ex.Message);
        }
    }

    private static string TrimTrailingSeparators(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
