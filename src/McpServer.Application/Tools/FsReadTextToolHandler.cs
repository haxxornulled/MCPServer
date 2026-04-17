using LanguageExt;
using McpServer.Application.Abstractions;
using McpServer.Application.Files;
using McpServer.Application.Files.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Tools
{
    public class FsReadTextToolHandler : IToolHandler<FsReadTextRequest>
    {
        private readonly IFileSystemService _fileService;
        private readonly ILogger<FsReadTextToolHandler> _logger;

        public FsReadTextToolHandler(IFileSystemService fileService, ILogger<FsReadTextToolHandler> logger)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Fin<Unit>> HandleAsync(FsReadTextRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Handling FsReadTextTool request for path: {Path}", request.Path);

            var command = new ReadFileTextCommand(request.Path, request.Encoding);
            
            var result = await _fileService.ReadTextAsync(command, ct);
            
            if (result.IsFaulted)
            {
                _logger.LogError("Failed to read text from file: {Message}", result.Error.Message);
                return Fin<Unit>.Fail(result.Error);
            }

            _logger.LogInformation("Successfully read text from file: {Path}", request.Path);
            return Fin<Unit>.Succ(Unit.Default);
        }
    }
}