using Microsoft.Extensions.Logging;
using McpServer.Protocol.Session;

namespace McpServer.Protocol.Lifecycle;

public sealed class ExitHandler(ILogger<ExitHandler> logger)
{
    public bool Handle(McpSession session)
    {
        if (session.IsShutdownRequested)
        {
            logger.LogInformation("Received exit after shutdown request");
            return true;
        }

        logger.LogWarning("Received exit without prior shutdown request");
        return true;
    }
}
