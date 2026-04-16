using LanguageExt;
using LanguageExt.Common;
using McpServer.Application.Abstractions.Web;
using static LanguageExt.Prelude;

namespace McpServer.Infrastructure.Web;

public sealed class WebPolicy(IReadOnlySet<string>? allowedHosts = null) : IWebPolicy
{
    private readonly IReadOnlySet<string>? _allowedHosts = allowedHosts;

    public int MaxResponseBytes => 512 * 1024;
    public TimeSpan DefaultTimeout => TimeSpan.FromSeconds(15);
    public int MaxRedirects => 5;

    public Fin<Unit> ValidateUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return Error.New($"Invalid URL: {url}");
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return Error.New("Only HTTP and HTTPS are supported.");
        }

        return ValidateHost(uri.Host);
    }

    public Fin<Unit> ValidateHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return Error.New("Host is required.");
        }

        if (_allowedHosts is null || _allowedHosts.Count == 0)
        {
            return unit;
        }

        return _allowedHosts.Contains(host) ? unit : Error.New($"Host not allowed: {host}");
    }
}
