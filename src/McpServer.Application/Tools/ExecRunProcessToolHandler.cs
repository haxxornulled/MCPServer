using LanguageExt;
using McpServer.Application.Abstractions;
using McpServer.Application.Execution;
using McpServer.Application.Execution.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Tools
{
    public class ExecRunProcessToolHandler : IToolHandler<ExecRunProcessRequest>
    {
        private readonly IProcessService _processService;
        private readonly ILogger<ExecRunProcessToolHandler> _logger;

        public ExecRunProcessToolHandler(IProcessService processService, ILogger<ExecRunProcessToolHandler> logger)
        {
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Fin<Unit>> HandleAsync(ExecRunProcessRequest request, CancellationToken ct)
        {
            _logger.LogInformation("Handling ExecRunProcessTool request for command: {Command}", request.Command);

            var command = new RunProcessCommand(request.Command, request.Arguments, request.WorkingDirectory, request.TimeoutSeconds);
            
            var result = await _processService.RunAsync(command, ct);
            
            if (result.IsFaulted)
            {
                _logger.LogError("Failed to run process: {Message}", result.Error.Message);
                return Fin<Unit>.Fail(result.Error);
            }

            _logger.LogInformation("Successfully ran process: {Command}", request.Command);
            return Fin<Unit>.Succ(Unit.Default);
        }
    }
}