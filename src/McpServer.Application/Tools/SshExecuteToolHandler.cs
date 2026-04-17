using LanguageExt;
using McpServer.Application.Abstractions;
using McpServer.Application.Ssh;
using McpServer.Application.Ssh.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Tools
{
    public class SshExecuteToolHandler : IToolHandler<SshExecuteRequest>
    {
        private readonly ISshService _sshService;
        private readonly ILogger<SshExecuteToolHandler> _logger;

        public SshExecuteToolHandler(ISshService sshService, ILogger<SshExecuteToolHandler> logger)
        {
            _sshService = sshService ?? throw new ArgumentNullException(nameof(sshService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Fin<Unit>> HandleAsync(SshExecuteRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Handling SshExecuteTool request for profile: {Profile}", request.Profile);

            var command = new ExecuteSshCommand(request.Profile, request.Command, request.WorkingDirectory);
            
            var result = await _sshService.ExecuteAsync(command, ct);
            
            if (result.IsFaulted)
            {
                _logger.LogError("Failed to execute SSH command: {Message}", result.Error.Message);
                return Fin<Unit>.Fail(result.Error);
            }

            _logger.LogInformation("Successfully executed SSH command on profile: {Profile}", request.Profile);
            return Fin<Unit>.Succ(Unit.Default);
        }
    }
}