using LanguageExt;
using McpServer.Application.Abstractions;
using McpServer.Application.Execution;
using McpServer.Application.Execution.Commands;
using McpServer.Application.Execution.Results;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Services
{
    public class ProcessService : IProcessService
    {
        private readonly ILogger<ProcessService> _logger;

        public ProcessService(ILogger<ProcessService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Fin<ProcessExecutionResult>> RunAsync(RunProcessCommand command, CancellationToken ct)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(command.Command))
            {
                _logger.LogWarning("RunAsync called with invalid command: {Command}", command.Command);
                return Fin<ProcessExecutionResult>.Fail(new Error($"Invalid command: {command.Command}"));
            }

            try
            {
                _logger.LogInformation("Running process: {Command} with arguments: {Arguments}", command.Command, command.Arguments);

                // In a real implementation, this would start actual processes
                // For now, we'll simulate the operation with sample results
                var result = new ProcessExecutionResult(
                    Command: command.Command,
                    Arguments: command.Arguments,
                    WorkingDirectory: command.WorkingDirectory,
                    ExitCode: 0,
                    StandardOutput: $"Simulated output for command: {command.Command}",
                    StandardError: string.Empty,
                    TimedOut: false,
                    OutputTruncated: false);

                _logger.LogInformation("Successfully ran process: {Command}", command.Command);
                
                return Fin<ProcessExecutionResult>.Succ(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run process: {Command}", command.Command);
                return Fin<ProcessExecutionResult>.Fail(new Error($"Failed to run process: {ex.Message}"));
            }
        }
    }
}
