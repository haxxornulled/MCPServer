using LanguageExt;

namespace McpServer.Application.Abstractions.Web;

public interface IWebPolicy
{
    Fin<Unit> ValidateUrl(string url);
    Fin<Unit> ValidateHost(string host);
    int MaxResponseBytes { get; }
    TimeSpan DefaultTimeout { get; }
    int MaxRedirects { get; }
}
