using LanguageExt;
using McpServer.Application.Abstractions;
using McpServer.Application.Files;
using McpServer.Application.Files.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Tools
{
    public class FsCopyPathToolHandler : IToolHandler<FsCopyPathRequest>
    {
        private readonly IFileSystemService _fileService;
        private readonly ILogger<FsCopyPathToolHandler> _logger;

        public FsCopyPathToolHandler(IFileSystemService fileService, ILogger<FsCopyPathToolHandler> logger)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Fin<Unit>> HandleAsync(FsCopyPathRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Handling FsCopyPathTool request from {SourcePath} to {DestinationPath}", request.SourcePath, request.DestinationPath);

            var command = new CopyPathCommand(request.SourcePath, request.DestinationPath, request.Overwrite, request.Recursive);
            
            var result = await _fileService.CopyPathAsync(command, ct);
            
            if (result.IsFaulted)
            {
                _logger.LogError("Failed to copy path: {Message}", result.Error.Message);
                return Fin<Unit>.Fail(result.Error);
            }

            _logger.LogInformation("Successfully copied path from {SourcePath} to {DestinationPath}", request.SourcePath, request.DestinationPath);
            return Fin<Unit>.Succ(Unit.Default);
        }
    }
}