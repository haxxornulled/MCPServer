using LanguageExt;
using McpServer.Application.Abstractions;
using McpServer.Application.Ssh;
using McpServer.Application.Ssh.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Tools
{
    public class SshWriteTextToolHandler : IToolHandler<SshWriteTextRequest>
    {
        private readonly ISshService _sshService;
        private readonly ILogger<SshWriteTextToolHandler> _logger;

        public SshWriteTextToolHandler(ISshService sshService, ILogger<SshWriteTextToolHandler> logger)
        {
            _sshService = sshService ?? throw new ArgumentNullException(nameof(sshService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Fin<Unit>> HandleAsync(SshWriteTextRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Handling SshWriteTextTool request for profile: {Profile}, path: {Path}", request.Profile, request.Path);

            var command = new WriteSshTextCommand(request.Profile, request.Path, request.Content);
            
            var result = await _sshService.WriteTextAsync(command, ct);
            
            if (result.IsFaulted)
            {
                _logger.LogError("Failed to write text via SSH: {Message}", result.Error.Message);
                return Fin<Unit>.Fail(result.Error);
            }

            _logger.LogInformation("Successfully wrote text via SSH to profile: {Profile}, path: {Path}", request.Profile, request.Path);
            return Fin<Unit>.Succ(Unit.Default);
        }
    }
}