using System.Security.Cryptography;
using System.Text;
using LanguageExt;
using LanguageExt.Common;
using McpServer.Application.Abstractions.Ssh;
using McpServer.Application.Ssh.Commands;
using McpServer.Application.Ssh.Results;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace McpServer.Infrastructure.Ssh;

public sealed class SshService(
    IEnumerable<ConfiguredSshProfile> profiles,
    string contentRoot,
    ILogger<SshService> logger) : ISshService
{
    private const int MinimumTimeoutSeconds = 1;
    private const int MaximumTimeoutSeconds = 1800;
    private const int MinimumOutputChars = 256;
    private const int MaximumOutputChars = 200000;

    private readonly Dictionary<string, ConfiguredSshProfile> _profiles = profiles
        .Where(static profile => !string.IsNullOrWhiteSpace(profile.Name))
        .ToDictionary(static profile => profile.Name, StringComparer.OrdinalIgnoreCase);

    public async ValueTask<Fin<SshCommandResult>> ExecuteAsync(ExecuteSshCommand command, CancellationToken ct)
    {
        try
        {
            var profile = ResolveProfile(command.Profile);
            if (profile.IsFail)
            {
                return PropagateFailure<SshCommandResult>(profile);
            }

            var resolvedProfile = profile.Match(
                Succ: value => value,
                Fail: _ => throw new InvalidOperationException("Expected SSH profile resolution to succeed."));

            var timeoutSeconds = Math.Clamp(command.TimeoutSeconds, MinimumTimeoutSeconds, MaximumTimeoutSeconds);
            var maxOutputChars = Math.Clamp(command.MaxOutputChars, MinimumOutputChars, MaximumOutputChars);
            var workingDirectory = string.IsNullOrWhiteSpace(command.WorkingDirectory)
                ? resolvedProfile.WorkingDirectory ?? string.Empty
                : command.WorkingDirectory;

            var executionResult = await Task.Run(() =>
            {
                using var client = CreateSshClient(resolvedProfile, timeoutSeconds);
                client.Connect();

                using var sshCommand = client.CreateCommand(BuildRemoteCommand(command.Command, workingDirectory));
                sshCommand.CommandTimeout = TimeSpan.FromSeconds(timeoutSeconds);

                try
                {
                    var stdout = sshCommand.Execute();
                    var stderr = sshCommand.Error;
                    var outputTruncated = false;

                    stdout = Truncate(stdout, maxOutputChars, ref outputTruncated);
                    stderr = Truncate(stderr, maxOutputChars, ref outputTruncated);

                    return new SshCommandResult(
                        resolvedProfile.Name,
                        resolvedProfile.Host,
                        resolvedProfile.Port,
                        resolvedProfile.Username,
                        command.Command,
                        string.IsNullOrWhiteSpace(workingDirectory) ? "." : workingDirectory,
                        sshCommand.ExitStatus ?? -1,
                        stdout,
                        stderr,
                        TimedOut: false,
                        OutputTruncated: outputTruncated);
                }
                catch (SshOperationTimeoutException ex)
                {
                    return new SshCommandResult(
                        resolvedProfile.Name,
                        resolvedProfile.Host,
                        resolvedProfile.Port,
                        resolvedProfile.Username,
                        command.Command,
                        string.IsNullOrWhiteSpace(workingDirectory) ? "." : workingDirectory,
                        ExitCode: -1,
                        StandardOutput: string.Empty,
                        StandardError: ex.Message,
                        TimedOut: true,
                        OutputTruncated: false);
                }
            }, ct).ConfigureAwait(false);

            logger.LogInformation(
                "Executed SSH command via profile {Profile} on {Host}:{Port} with exit code {ExitCode}",
                executionResult.Profile,
                executionResult.Host,
                executionResult.Port,
                executionResult.ExitCode);

            return executionResult;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed executing SSH command via profile {Profile}", command.Profile);
            return Error.New(ex.Message);
        }
    }

    public async ValueTask<Fin<SshWriteTextResult>> WriteTextAsync(WriteSshTextCommand command, CancellationToken ct)
    {
        try
        {
            var profile = ResolveProfile(command.Profile);
            if (profile.IsFail)
            {
                return PropagateFailure<SshWriteTextResult>(profile);
            }

            var resolvedProfile = profile.Match(
                Succ: value => value,
                Fail: _ => throw new InvalidOperationException("Expected SSH profile resolution to succeed."));

            var writeResult = await Task.Run(() =>
            {
                using var client = CreateSftpClient(resolvedProfile, timeoutSeconds: 60);
                client.Connect();

                var remotePath = NormalizeRemotePath(command.Path);
                if (string.IsNullOrWhiteSpace(remotePath))
                {
                    throw new InvalidOperationException("Remote path is required.");
                }

                var remoteDirectory = GetRemoteDirectory(remotePath);
                var createdDirectories = false;

                if (command.CreateDirectories && !string.IsNullOrWhiteSpace(remoteDirectory))
                {
                    createdDirectories = EnsureDirectoryExists(client, remoteDirectory) || createdDirectories;
                }

                var existed = client.Exists(remotePath);
                if (existed && !command.Overwrite)
                {
                    throw new InvalidOperationException($"Remote path already exists: {remotePath}");
                }

                var encoding = Encoding.GetEncoding(command.Encoding);
                var bytes = encoding.GetBytes(command.Content);
                using var ms = new MemoryStream(bytes, writable: false);
                client.UploadFile(ms, remotePath, canOverride: command.Overwrite);

                string? permissionsApplied = null;
                if (!string.IsNullOrWhiteSpace(command.Permissions))
                {
                    permissionsApplied = NormalizePermissions(command.Permissions);
                    client.ChangePermissions(remotePath, Convert.ToInt16(permissionsApplied, 8));
                }

                return new SshWriteTextResult(
                    resolvedProfile.Name,
                    resolvedProfile.Host,
                    resolvedProfile.Port,
                    resolvedProfile.Username,
                    remotePath,
                    bytes.LongLength,
                    Overwritten: existed,
                    CreatedDirectories: createdDirectories,
                    PermissionsApplied: permissionsApplied);
            }, ct).ConfigureAwait(false);

            logger.LogInformation(
                "Wrote remote file {Path} via SSH profile {Profile}",
                writeResult.Path,
                writeResult.Profile);

            return writeResult;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed writing remote file via SSH profile {Profile}", command.Profile);
            return Error.New(ex.Message);
        }
    }

    private Fin<ConfiguredSshProfile> ResolveProfile(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return Error.New("SSH profile is required.");
        }

        if (!_profiles.TryGetValue(profileName, out var profile))
        {
            return Error.New($"Unknown SSH profile: {profileName}");
        }

        if (string.IsNullOrWhiteSpace(profile.Host))
        {
            return Error.New($"SSH profile '{profileName}' is missing Host.");
        }

        if (string.IsNullOrWhiteSpace(profile.Username))
        {
            return Error.New($"SSH profile '{profileName}' is missing Username.");
        }

        return profile;
    }

    private SshClient CreateSshClient(ConfiguredSshProfile profile, int timeoutSeconds)
    {
        var connectionInfo = CreateConnectionInfo(profile, timeoutSeconds);
        var client = new SshClient(connectionInfo);
        ConfigureHostKeyValidation(client, profile);
        return client;
    }

    private SftpClient CreateSftpClient(ConfiguredSshProfile profile, int timeoutSeconds)
    {
        var connectionInfo = CreateConnectionInfo(profile, timeoutSeconds);
        var client = new SftpClient(connectionInfo);
        ConfigureHostKeyValidation(client, profile);
        return client;
    }

    private ConnectionInfo CreateConnectionInfo(ConfiguredSshProfile profile, int timeoutSeconds)
    {
        List<AuthenticationMethod> authMethods = [];

        if (!string.IsNullOrWhiteSpace(profile.PasswordEnvironmentVariable))
        {
            var password = Environment.GetEnvironmentVariable(profile.PasswordEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    $"Environment variable '{profile.PasswordEnvironmentVariable}' for SSH profile '{profile.Name}' is not set.");
            }

            authMethods.Add(new PasswordAuthenticationMethod(profile.Username, password));
        }

        if (!string.IsNullOrWhiteSpace(profile.PrivateKeyPath))
        {
            var privateKeyPath = ResolvePath(profile.PrivateKeyPath);
            if (!File.Exists(privateKeyPath))
            {
                throw new FileNotFoundException($"SSH private key was not found: {privateKeyPath}", privateKeyPath);
            }

            var passphrase = string.IsNullOrWhiteSpace(profile.PrivateKeyPassphraseEnvironmentVariable)
                ? null
                : Environment.GetEnvironmentVariable(profile.PrivateKeyPassphraseEnvironmentVariable);

            var privateKeyFile = string.IsNullOrWhiteSpace(passphrase)
                ? new PrivateKeyFile(privateKeyPath)
                : new PrivateKeyFile(privateKeyPath, passphrase);

            authMethods.Add(new PrivateKeyAuthenticationMethod(profile.Username, privateKeyFile));
        }

        if (authMethods.Count is 0)
        {
            throw new InvalidOperationException(
                $"SSH profile '{profile.Name}' must define either PasswordEnvironmentVariable or PrivateKeyPath.");
        }

        return new ConnectionInfo(profile.Host, profile.Port, profile.Username, authMethods.ToArray())
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };
    }

    private void ConfigureHostKeyValidation(BaseClient client, ConfiguredSshProfile profile)
    {
        client.HostKeyReceived += (_, args) =>
        {
            args.CanTrust = profile.AcceptUnknownHostKey || HostKeyMatches(profile.HostKeySha256, args.HostKey);
        };
    }

    private static bool HostKeyMatches(string? expectedFingerprint, byte[] hostKey)
    {
        if (string.IsNullOrWhiteSpace(expectedFingerprint))
        {
            return false;
        }

        var normalizedExpected = expectedFingerprint
            .Trim()
            .Replace("SHA256:", string.Empty, StringComparison.OrdinalIgnoreCase)
            .TrimEnd('=');
        var computed = Convert.ToBase64String(SHA256.HashData(hostKey)).TrimEnd('=');
        return string.Equals(normalizedExpected, computed, StringComparison.Ordinal);
    }

    private string ResolvePath(string path) =>
        Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(contentRoot, path));

    private static string BuildRemoteCommand(string command, string? workingDirectory)
    {
        var script = string.IsNullOrWhiteSpace(workingDirectory)
            ? command
            : $"cd {QuotePosix(workingDirectory)} && {command}";

        return $"sh -lc {QuotePosix(script)}";
    }

    private static string QuotePosix(string value) =>
        $"'{value.Replace("'", "'\"'\"'")}'";

    private static string Truncate(string text, int maxChars, ref bool truncated)
    {
        if (text.Length <= maxChars)
        {
            return text;
        }

        truncated = true;
        const string suffix = "\n...[truncated]";
        var take = Math.Max(0, maxChars - suffix.Length);
        return text[..take] + suffix;
    }

    private static string NormalizeRemotePath(string path) => path.Replace('\\', '/').Trim();

    private static string GetRemoteDirectory(string path)
    {
        var index = path.LastIndexOf('/');
        return index <= 0 ? string.Empty : path[..index];
    }

    private static bool EnsureDirectoryExists(SftpClient client, string directory)
    {
        var normalized = NormalizeRemotePath(directory);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        var createdAny = false;
        var startsAtRoot = normalized.StartsWith("/", StringComparison.Ordinal);
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var current = startsAtRoot ? "/" : string.Empty;

        foreach (var segment in segments)
        {
            current = current switch
            {
                "/" => "/" + segment,
                "" => segment,
                _ => current + "/" + segment
            };

            if (!client.Exists(current))
            {
                client.CreateDirectory(current);
                createdAny = true;
            }
        }

        return createdAny;
    }

    private static string NormalizePermissions(string permissions)
    {
        var normalized = permissions.Trim();
        if (normalized.Length is < 3 or > 4 || normalized.Any(ch => ch is < '0' or > '7'))
        {
            throw new InvalidOperationException($"Invalid octal permissions value: {permissions}");
        }

        return normalized;
    }

    private static Fin<T> PropagateFailure<T>(Fin<ConfiguredSshProfile> failure) =>
        failure.Match<Fin<T>>(
            Succ: _ => throw new InvalidOperationException("Expected SSH profile resolution to fail."),
            Fail: error => error);
}