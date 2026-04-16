using LanguageExt;
using LanguageExt.Common;
using McpServer.Contracts.Lifecycle;
using static LanguageExt.Prelude;

namespace McpServer.Protocol.Session;

public sealed class McpSession
{
    private int _initializeCompleted;
    private int _ready;
    private int _shutdownRequested;

    public string? ProtocolVersion { get; private set; }
    public ClientCapabilitiesDto? ClientCapabilities { get; private set; }

    public bool IsInitialized => Volatile.Read(ref _initializeCompleted) == 1;
    public bool IsReady => Volatile.Read(ref _ready) == 1;
    public bool IsShutdownRequested => Volatile.Read(ref _shutdownRequested) == 1;

    public Fin<Unit> CompleteInitialize(string protocolVersion, ClientCapabilitiesDto? clientCapabilities)
    {
        if (Interlocked.Exchange(ref _initializeCompleted, 1) == 1)
        {
            return Error.New("Session already initialized");
        }

        ProtocolVersion = protocolVersion;
        ClientCapabilities = clientCapabilities;
        return unit;
    }

    public Fin<Unit> MarkReady()
    {
        if (!IsInitialized)
        {
            return Error.New("Initialize must complete first");
        }

        if (Interlocked.Exchange(ref _ready, 1) == 1)
        {
            return Error.New("Session already ready");
        }

        return unit;
    }

    public Fin<Unit> RequestShutdown()
    {
        if (!IsInitialized)
        {
            return Error.New("Cannot shutdown before initialization");
        }

        if (Interlocked.Exchange(ref _shutdownRequested, 1) == 1)
        {
            return Error.New("Shutdown already requested");
        }

        return unit;
    }
}
