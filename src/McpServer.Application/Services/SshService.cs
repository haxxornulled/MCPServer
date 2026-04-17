using LanguageExt;
using McpServer.Application.Abstractions;
using McpServer.Application.Ssh;
using McpServer.Application.Ssh.Commands;
using McpServer.Application.Ssh.Results;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Services
{
    public class SshService : ISshService
    {
        private readonly ILogger<SshService> _logger;

        public SshService(ILogger<SshService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Fin<SshCommandResult>> ExecuteAsync(ExecuteSshCommand command, CancellationToken ct)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(command.Profile))
            {
                _logger.LogWarning("ExecuteAsync called with invalid profile: {Profile}", command.Profile);
                return Fin<SshCommandResult>.Fail(new Error($"Invalid profile: {command.Profile}"));
            }

            if (string.IsNullOrWhiteSpace(command.Command))
            {
                _logger.LogWarning("ExecuteAsync called with invalid command: {Command}", command.Command);
                return Fin<SshCommandResult>.Fail(new Error($"Invalid command: {command.Command}"));
            }

            try
            {
                _logger.LogInformation("Executing SSH command on profile '{Profile}': {Command}", command.Profile, command.Command);

                // In a real implementation, this would use SSH libraries like SSH.NET
                // For now, we'll simulate the operation
                var result = new SshCommandResult(
                    Profile: command.Profile,
                    Host: "localhost", // This would come from profile in real implementation
                    Port: 22, // This would come from profile in real implementation
                    Username: "user", // This would come from profile in real implementation
                    Command: command.Command,
                    WorkingDirectory: command.WorkingDirectory,
                    ExitCode: 0,
                    StandardOutput: $"Simulated output for command: {command.Command}",
                    StandardError: string.Empty,
                    TimedOut: false,
                    OutputTruncated: false);

                _logger.LogInformation("Successfully executed SSH command on profile '{Profile}'", command.Profile);
                
                return Fin<SshCommandResult>.Succ(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute SSH command on profile '{Profile}': {Command}", command.Profile, command.Command);
                return Fin<SshCommandResult>.Fail(new Error($"Failed to execute SSH command: {ex.Message}"));
            }
        }

        public async ValueTask<Fin<SshFileWriteResult>> WriteTextAsync(WriteSshTextCommand command, CancellationToken ct)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(command.Profile))
            {
                _logger.LogWarning("WriteTextAsync called with invalid profile: {Profile}", command.Profile);
                return Fin<SshFileWriteResult>.Fail(new Error($"Invalid profile: {command.Profile}"));
            }

            if (string.IsNullOrWhiteSpace(command.Path))
            {
                _logger.LogWarning("WriteTextAsync called with invalid path: {Path}", command.Path);
                return Fin<SshFileWriteResult>.Fail(new Error($"Invalid path: {command.Path}"));
            }

            try
            {
                _logger.LogInformation("Writing text to SSH file on profile '{Profile}', path: {Path}", command.Profile, command.Path);

                // In a real implementation, this would use SSH libraries like SSH.NET
                // For now, we'll simulate the operation
                var result = new SshFileWriteResult(
                    Profile: command.Profile,
                    Host: "localhost", // This would come from profile in real implementation
                    Port: 22, // This would come from profile in real implementation
                    Username: "user", // This would come from profile in real implementation
                    Path: command.Path,
                    BytesWritten: command.Content.Length,
                    Success: true);

                _logger.LogInformation("Successfully wrote text to SSH file on profile '{Profile}', path: {Path}", command.Profile, command.Path);
                
                return Fin<SshFileWriteResult>.Succ(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write text to SSH file on profile '{Profile}', path: {Path}", command.Profile, command.Path);
                return Fin<SshFileWriteResult>.Fail(new Error($"Failed to write SSH file: {ex.Message}"));
            }
        }
    }
}
