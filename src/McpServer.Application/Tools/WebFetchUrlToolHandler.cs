using LanguageExt;
using McpServer.Application.Abstractions;
using McpServer.Application.Web;
using McpServer.Application.Web.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Tools
{
    public class WebFetchUrlToolHandler : IToolHandler<WebFetchUrlRequest>
    {
        private readonly IWebService _webService;
        private readonly ILogger<WebFetchUrlToolHandler> _logger;

        public WebFetchUrlToolHandler(IWebService webService, ILogger<WebFetchUrlToolHandler> logger)
        {
            _webService = webService ?? throw new ArgumentNullException(nameof(webService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Fin<Unit>> HandleAsync(WebFetchUrlRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Handling WebFetchUrlTool request for URL: {Url}", request.Url);

            var command = new FetchUrlCommand(request.Url, true, null, request.TimeoutSeconds);
            
            var result = await _webService.FetchUrlAsync(command, ct);
            
            if (result.IsFaulted)
            {
                _logger.LogError("Failed to fetch URL: {Message}", result.Error.Message);
                return Fin<Unit>.Fail(result.Error);
            }

            _logger.LogInformation("Successfully fetched URL: {Url}", request.Url);
            return Fin<Unit>.Succ(Unit.Default);
        }
    }
}
