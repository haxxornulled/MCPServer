using LanguageExt;
using McpServer.Application.Abstractions;
using McpServer.Application.Files;
using McpServer.Application.Files.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Tools
{
    public class FsAppendTextToolHandler : IToolHandler<AppendFileTextRequest>
    {
        private readonly IFileSystemService _fileService;
        private readonly ILogger<FsAppendTextToolHandler> _logger;

        public FsAppendTextToolHandler(IFileSystemService fileService, ILogger<FsAppendTextToolHandler> logger)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Fin<Unit>> HandleAsync(AppendFileTextRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Handling FsAppendTextTool request for path: {Path}", request.Path);

            var command = new AppendFileTextCommand(request.Path, request.Content, request.Encoding, request.Flush);
            
            var result = await _fileService.AppendTextAsync(command, ct);
            
            if (result.IsFaulted)
            {
                _logger.LogError("Failed to append text to file: {Message}", result.Error.Message);
                return Fin<Unit>.Fail(result.Error);
            }

            _logger.LogInformation("Successfully appended text to file: {Path}", request.Path);
            return Fin<Unit>.Succ(Unit.Default);
        }
    }
}
