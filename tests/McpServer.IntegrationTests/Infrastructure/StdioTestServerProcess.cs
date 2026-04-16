using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace McpServer.IntegrationTests.Infrastructure;

public sealed class StdioTestServerProcess : IAsyncDisposable
{
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
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" --no-build",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false),
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start MCP server process.");
        }

        await Task.Delay(250, ct).ConfigureAwait(false);
        return new StdioTestServerProcess(process);
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
}
