using LanguageExt;
using McpServer.Application.Abstractions;
using McpServer.Application.Files;
using McpServer.Application.Files.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Tools
{
    public class FsMovePathToolHandler : IToolHandler<FsMovePathRequest>
    {
        private readonly IFileSystemService _fileService;
        private readonly ILogger<FsMovePathToolHandler> _logger;

        public FsMovePathToolHandler(IFileSystemService fileService, ILogger<FsMovePathToolHandler> logger)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Fin<Unit>> HandleAsync(FsMovePathRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Handling FsMovePathTool request from {SourcePath} to {DestinationPath}", request.SourcePath, request.DestinationPath);

            var command = new MovePathCommand(request.SourcePath, request.DestinationPath, request.Overwrite);
            
            var result = await _fileService.MovePathAsync(command, ct);
            
            if (result.IsFaulted)
            {
                _logger.LogError("Failed to move path: {Message}", result.Error.Message);
                return Fin<Unit>.Fail(result.Error);
            }

            _logger.LogInformation("Successfully moved path from {SourcePath} to {DestinationPath}", request.SourcePath, request.DestinationPath);
            return Fin<Unit>.Succ(Unit.Default);
        }
    }
}