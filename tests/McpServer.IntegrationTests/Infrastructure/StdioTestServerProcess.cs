using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace McpServer.IntegrationTests.Infrastructure;

public sealed class StdioTestServerProcess : IAsyncDisposable
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMilliseconds(250);
    private readonly Process _process;
    private readonly CancellationTokenSource _stderrPumpCts = new();
    private readonly Task _stderrPumpTask;
    private readonly ConcurrentQueue<string> _stderrLines = new();

    public StreamWriter Input { get; }
    public StreamReader Output { get; }
    public StreamReader Error { get; }

    public bool IsAlive => !_process.HasExited;
    public IReadOnlyCollection<string> StandardErrorLines => _stderrLines.ToArray();

    private StdioTestServerProcess(Process process)
    {
        _process = process;
        Input = process.StandardInput;
        Output = process.StandardOutput;
        Error = process.StandardError;
        _stderrPumpTask = Task.Run(() => PumpStandardErrorAsync(_stderrPumpCts.Token));
    }

    public static async Task<StdioTestServerProcess> StartAsync(string projectPath, CancellationToken ct = default)
        => await StartAsync(projectPath, workingDirectory: null, ct).ConfigureAwait(false);

    public static async Task<StdioTestServerProcess> StartAsync(
        string projectPath,
        string? workingDirectory,
        CancellationToken ct = default)
    {
        var configuration = GetCurrentConfiguration();
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" -c {configuration} --no-build",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false),
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            psi.WorkingDirectory = workingDirectory;
        }

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start MCP server process.");
        }

        await Task.Delay(StartupDelay, ct).ConfigureAwait(false);

        var server = new StdioTestServerProcess(process);

        if (process.HasExited)
        {
            throw new InvalidOperationException($"MCP server exited during startup. {server.GetStandardErrorSummary()}");
        }

        return server;
    }

    private static string GetCurrentConfiguration()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var pathSegment = $"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}";

        return baseDirectory.Contains(pathSegment, StringComparison.OrdinalIgnoreCase)
            ? "Release"
            : "Debug";
    }

    private async Task PumpStandardErrorAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await Error.ReadLineAsync(ct).ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                _stderrLines.Enqueue(line);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _stderrPumpCts.Cancel();

            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync().ConfigureAwait(false);
            }

            try
            {
                await _stderrPumpTask.ConfigureAwait(false);
            }
            catch
            {
            }
        }
        finally
        {
            _stderrPumpCts.Dispose();
            _process.Dispose();
        }
    }

    private string GetStandardErrorSummary()
    {
        var lines = _stderrLines.ToArray();
        if (lines.Length is 0)
        {
            return "No stderr output was captured.";
        }

        return string.Join(Environment.NewLine, lines);
    }
}
