using LanguageExt;
using McpServer.Application.Abstractions;
using McpServer.Application.Files;
using McpServer.Application.Files.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Tools
{
    public class FsDeletePathToolHandler : IToolHandler<FsDeletePathRequest>
    {
        private readonly IFileSystemService _fileService;
        private readonly ILogger<FsDeletePathToolHandler> _logger;

        public FsDeletePathToolHandler(IFileSystemService fileService, ILogger<FsDeletePathToolHandler> logger)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Fin<Unit>> HandleAsync(FsDeletePathRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Handling FsDeletePathTool request for path: {Path}", request.Path);

            var command = new DeletePathCommand(request.Path, request.Recursive);
            
            var result = await _fileService.DeletePathAsync(command, ct);
            
            if (result.IsFaulted)
            {
                _logger.LogError("Failed to delete path: {Message}", result.Error.Message);
                return Fin<Unit>.Fail(result.Error);
            }

            _logger.LogInformation("Successfully deleted path: {Path}", request.Path);
            return Fin<Unit>.Succ(Unit.Default);
        }
    }
}