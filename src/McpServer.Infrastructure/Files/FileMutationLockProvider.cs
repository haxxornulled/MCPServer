using System.Collections.Concurrent;
using System.Diagnostics;
using McpServer.Application.Abstractions.Files;
using Microsoft.Extensions.Logging;

namespace McpServer.Infrastructure.Files;

public sealed class FileMutationLockProvider(
    ILogger<FileMutationLockProvider> logger) : IFileMutationLockProvider
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(PathComparison.Comparer);

    public async ValueTask<IAsyncDisposable> AcquireAsync(string normalizedPath, CancellationToken ct)
    {
        var gate = _locks.GetOrAdd(normalizedPath, static _ => new SemaphoreSlim(1, 1));
        var started = Stopwatch.GetTimestamp();

        await gate.WaitAsync(ct).ConfigureAwait(false);

        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        if (elapsedMs >= 25)
        {
            logger.LogWarning(
                "Write lock wait detected for {NormalizedPath} after {ElapsedMs}ms",
                normalizedPath,
                elapsedMs);
        }

        return new Releaser(gate);
    }

    public async ValueTask<IAsyncDisposable> AcquireManyAsync(IEnumerable<string> normalizedPaths, CancellationToken ct)
    {
        var ordered = normalizedPaths
            .Distinct(PathComparison.Comparer)
            .OrderBy(x => x, PathComparison.Comparer)
            .ToArray();

        var releasers = new List<IAsyncDisposable>(ordered.Length);

        try
        {
            foreach (var path in ordered)
            {
                var releaser = await AcquireAsync(path, ct).ConfigureAwait(false);
                releasers.Add(releaser);
            }

            return new CompositeReleaser(releasers);
        }
        catch
        {
            for (var i = releasers.Count - 1; i >= 0; i--)
            {
                await releasers[i].DisposeAsync().ConfigureAwait(false);
            }

            throw;
        }
    }

    private sealed class Releaser(SemaphoreSlim gate) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            gate.Release();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CompositeReleaser(IReadOnlyList<IAsyncDisposable> releasers) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            for (var i = releasers.Count - 1; i >= 0; i--)
            {
                await releasers[i].DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
