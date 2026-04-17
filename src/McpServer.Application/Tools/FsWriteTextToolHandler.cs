using LanguageExt;
using McpServer.Application.Abstractions;
using McpServer.Application.Files;
using McpServer.Application.Files.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Tools
{
    public class FsWriteTextToolHandler : IToolHandler<FsWriteTextRequest>
    {
        private readonly IFileSystemService _fileService;
        private readonly ILogger<FsWriteTextToolHandler> _logger;

        public FsWriteTextToolHandler(IFileSystemService fileService, ILogger<FsWriteTextToolHandler> logger)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Fin<Unit>> HandleAsync(FsWriteTextRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Handling FsWriteTextTool request for path: {Path}", request.Path);

            var command = new WriteFileTextCommand(request.Path, request.Content, request.Encoding);
            
            var result = await _fileService.WriteTextAsync(command, ct);
            
            if (result.IsFaulted)
            {
                _logger.LogError("Failed to write text to file: {Message}", result.Error.Message);
                return Fin<Unit>.Fail(result.Error);
            }

            _logger.LogInformation("Successfully wrote text to file: {Path}", request.Path);
            return Fin<Unit>.Succ(Unit.Default);
        }
    }
}