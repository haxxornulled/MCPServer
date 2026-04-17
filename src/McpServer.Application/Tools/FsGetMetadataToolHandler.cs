using LanguageExt;
using McpServer.Application.Abstractions;
using McpServer.Application.Files;
using McpServer.Application.Files.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Tools
{
    public class FsGetMetadataToolHandler : IToolHandler<FsGetMetadataRequest>
    {
        private readonly IFileSystemService _fileService;
        private readonly ILogger<FsGetMetadataToolHandler> _logger;

        public FsGetMetadataToolHandler(IFileSystemService fileService, ILogger<FsGetMetadataToolHandler> logger)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Fin<Unit>> HandleAsync(FsGetMetadataRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Handling FsGetMetadataTool request for path: {Path}", request.Path);

            var command = new GetMetadataCommand(request.Path);
            
            var result = await _fileService.GetMetadataAsync(command, ct);
            
            if (result.IsFaulted)
            {
                _logger.LogError("Failed to get metadata: {Message}", result.Error.Message);
                return Fin<Unit>.Fail(result.Error);
            }

            _logger.LogInformation("Successfully got metadata for path: {Path}", request.Path);
            return Fin<Unit>.Succ(Unit.Default);
        }
    }
}