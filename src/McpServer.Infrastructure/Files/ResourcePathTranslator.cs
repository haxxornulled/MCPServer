using LanguageExt;
using LanguageExt.Common;
using McpServer.Application.Abstractions.Files;

namespace McpServer.Infrastructure.Files;

public sealed class ResourcePathTranslator : IResourcePathTranslator
{
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
                "file" or "dir" or "filemeta" => parsed.LocalPath,
                _ => Error.New($"Unsupported URI scheme: {parsed.Scheme}")
            };
        }
        catch (Exception ex)
        {
            return Error.New(ex.Message);
        }
    }
}
