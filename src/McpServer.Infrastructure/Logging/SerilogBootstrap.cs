using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace McpServer.Infrastructure.Logging;

public static class SerilogBootstrap
{
    public static void Configure(LoggerConfiguration configuration, IConfiguration appConfiguration)
    {
        configuration
            .ReadFrom.Configuration(appConfiguration)
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(standardErrorFromLevel: LogEventLevel.Verbose)
            .WriteTo.File(
                path: "logs/mcp-server-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1));
    }
}
