using LanguageExt;
using McpServer.Application.Abstractions;
using McpServer.Application.Files;
using McpServer.Application.Files.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Tools
{
    public class FsCreateDirectoryToolHandler : IToolHandler<FsCreateDirectoryRequest>
    {
        private readonly IFileSystemService _fileService;
        private readonly ILogger<FsCreateDirectoryToolHandler> _logger;

        public FsCreateDirectoryToolHandler(IFileSystemService fileService, ILogger<FsCreateDirectoryToolHandler> logger)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Fin<Unit>> HandleAsync(FsCreateDirectoryRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Handling FsCreateDirectoryTool request for path: {Path}", request.Path);

            var command = new CreateDirectoryCommand(request.Path);
            
            var result = await _fileService.CreateDirectoryAsync(command, ct);
            
            if (result.IsFaulted)
            {
                _logger.LogError("Failed to create directory: {Message}", result.Error.Message);
                return Fin<Unit>.Fail(result.Error);
            }

            _logger.LogInformation("Successfully created directory: {Path}", request.Path);
            return Fin<Unit>.Succ(Unit.Default);
        }
    }
}