using LanguageExt;
using McpServer.Application.Abstractions;
using McpServer.Application.Files;
using McpServer.Application.Files.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Tools
{
    public class FsListDirectoryToolHandler : IToolHandler<FsListDirectoryRequest>
    {
        private readonly IFileSystemService _fileService;
        private readonly ILogger<FsListDirectoryToolHandler> _logger;

        public FsListDirectoryToolHandler(IFileSystemService fileService, ILogger<FsListDirectoryToolHandler> logger)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Fin<Unit>> HandleAsync(FsListDirectoryRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Handling FsListDirectoryTool request for path: {Path}", request.Path);

            var command = new ListDirectoryCommand(request.Path, request.SearchPattern);
            
            var result = await _fileService.ListDirectoryAsync(command, ct);
            
            if (result.IsFaulted)
            {
                _logger.LogError("Failed to list directory: {Message}", result.Error.Message);
                return Fin<Unit>.Fail(result.Error);
            }

            _logger.LogInformation("Successfully listed directory: {Path}", request.Path);
            return Fin<Unit>.Succ(Unit.Default);
        }
    }
}