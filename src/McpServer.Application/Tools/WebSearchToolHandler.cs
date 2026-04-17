using LanguageExt;
using McpServer.Application.Abstractions;
using McpServer.Application.Web;
using McpServer.Application.Web.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Tools
{
    public class WebSearchToolHandler : IToolHandler<WebSearchRequest>
    {
        private readonly IWebService _webService;
        private readonly ILogger<WebSearchToolHandler> _logger;

        public WebSearchToolHandler(IWebService webService, ILogger<WebSearchToolHandler> logger)
        {
            _webService = webService ?? throw new ArgumentNullException(nameof(webService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Fin<Unit>> HandleAsync(WebSearchRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Handling WebSearchTool request for query: {Query}", request.Query);

            var command = new SearchWebCommand(request.Query);
            
            var result = await _webService.SearchAsync(command, ct);
            
            if (result.IsFaulted)
            {
                _logger.LogError("Failed to search web: {Message}", result.Error.Message);
                return Fin<Unit>.Fail(result.Error);
            }

            _logger.LogInformation("Successfully searched web for query: {Query}", request.Query);
            return Fin<Unit>.Succ(Unit.Default);
        }
    }
}