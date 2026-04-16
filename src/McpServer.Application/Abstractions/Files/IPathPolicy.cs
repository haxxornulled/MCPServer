using LanguageExt;

namespace McpServer.Application.Abstractions.Files;

public interface IPathPolicy
{
    Fin<string> NormalizeAndValidateReadPath(string rawPath);
    Fin<string> NormalizeAndValidateWritePath(string rawPath);
}
