using System.Diagnostics;
using System.Text.Json;
using Autofac;
using McpServer.Contracts.Lifecycle;
using McpServer.Contracts.Prompts;
using McpServer.Contracts.Resources;
using McpServer.Contracts.Tools;
using McpServer.Protocol;
using McpServer.Protocol.JsonRpc;
using McpServer.Protocol.Lifecycle;
using McpServer.Protocol.Routing;
using McpServer.Protocol.Session;

namespace McpServer.Host.Transport.Stdio;

public sealed class StdioServerHostedService(
    ILifetimeScope scope,
    ILogger<StdioServerHostedService> logger,
    IHostApplicationLifetime applicationLifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MCP server starting (stdio)");

        await using var transport = new StdioMessageTransport(
            Console.OpenStandardInput(),
            Console.OpenStandardOutput(),
            scope.Resolve<ILogger<StdioMessageTransport>>());

        var session = scope.Resolve<McpSession>();
        var initializeHandler = scope.Resolve<InitializeHandler>();
        var shutdownHandler = scope.Resolve<ShutdownHandler>();
        var exitHandler = scope.Resolve<ExitHandler>();
        var toolRouter = scope.Resolve<ToolCallRouter>();
        var resourceRouter = scope.Resolve<ResourceReadRouter>();
        var promptRouter = scope.Resolve<PromptRouter>();

        while (!stoppingToken.IsCancellationRequested)
        {
            var request = await transport.ReadRequestAsync(stoppingToken).ConfigureAwait(false);
            if (request is null)
            {
                continue;
            }

            using var logScope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["RequestId"] = request.Id?.ToString(),
                ["Method"] = request.Method
            });

            var started = Stopwatch.GetTimestamp();

            var dispatch = await DispatchAsync(
                request,
                session,
                initializeHandler,
                shutdownHandler,
                exitHandler,
                toolRouter,
                resourceRouter,
                promptRouter,
                stoppingToken).ConfigureAwait(false);

            if (dispatch.Response is not null)
            {
                await transport.WriteResponseAsync(dispatch.Response, stoppingToken).ConfigureAwait(false);
            }

            logger.LogInformation(
                "Handled {Method} in {ElapsedMs}ms",
                request.Method,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds);

            if (dispatch.ShouldExit)
            {
                logger.LogInformation("Stopping host after exit notification");
                applicationLifetime.StopApplication();
                break;
            }
        }

        logger.LogInformation("MCP server stopping");
    }

    private static async ValueTask<DispatchResult> DispatchAsync(
        JsonRpcRequest request,
        McpSession session,
        InitializeHandler initializeHandler,
        ShutdownHandler shutdownHandler,
        ExitHandler exitHandler,
        ToolCallRouter toolRouter,
        ResourceReadRouter resourceRouter,
        PromptRouter promptRouter,
        CancellationToken ct)
    {
        try
        {
            return request.Method switch
            {
                "initialize" => DispatchResult.WithResponse(HandleInitialize(request, session, initializeHandler)),
                "notifications/initialized" => DispatchResult.WithResponse(HandleInitialized(request, session)),
                "shutdown" => DispatchResult.WithResponse(HandleShutdown(request, session, shutdownHandler)),
                "exit" => DispatchResult.Exit(HandleExit(session, exitHandler)),
                "tools/list" => DispatchResult.WithResponse(
                    RequireReady(request, session, () => new JsonRpcResponse("2.0", request.Id, toolRouter.ListTools()))),
                "tools/call" => DispatchResult.WithResponse(
                    await HandleToolCallAsync(request, session, toolRouter, ct).ConfigureAwait(false)),
                "resources/list" => DispatchResult.WithResponse(
                    RequireReady(request, session, () => new JsonRpcResponse("2.0", request.Id, resourceRouter.ListResources()))),
                "resources/read" => DispatchResult.WithResponse(
                    await HandleResourceReadAsync(request, session, resourceRouter, ct).ConfigureAwait(false)),
                "prompts/list" => DispatchResult.WithResponse(
                    RequireReady(request, session, () => new JsonRpcResponse("2.0", request.Id, promptRouter.ListPrompts()))),
                "prompts/get" => DispatchResult.WithResponse(
                    await HandlePromptGetAsync(request, session, promptRouter, ct).ConfigureAwait(false)),
                _ => DispatchResult.WithResponse(JsonRpcErrorFactory.MethodNotFound(request.Id, request.Method))
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return DispatchResult.WithResponse(JsonRpcErrorFactory.InternalError(request.Id, ex.Message));
        }
    }

    private static JsonRpcResponse HandleInitialize(
        JsonRpcRequest request,
        McpSession session,
        InitializeHandler initializeHandler)
    {
        var payload = request.Params?.Deserialize<InitializeRequestDto>();
        if (payload is null)
        {
            return JsonRpcErrorFactory.InvalidParams(request.Id, "Invalid initialize params");
        }

        var result = initializeHandler.Handle(payload, session);

        return result.Match(
            Succ: x => new JsonRpcResponse("2.0", request.Id, x),
            Fail: e => JsonRpcErrorFactory.ServerError(request.Id, e.Message));
    }

    private static JsonRpcResponse? HandleInitialized(JsonRpcRequest request, McpSession session)
    {
        var result = session.MarkReady();

        return result.Match<JsonRpcResponse?>(
            Succ: _ => null,
            Fail: e => JsonRpcErrorFactory.ServerError(request.Id, e.Message));
    }

    private static JsonRpcResponse HandleShutdown(
        JsonRpcRequest request,
        McpSession session,
        ShutdownHandler shutdownHandler)
    {
        var result = shutdownHandler.Handle(session);

        return result.Match(
            Succ: _ => new JsonRpcResponse("2.0", request.Id, new Dictionary<string, object?>()),
            Fail: e => JsonRpcErrorFactory.ServerError(request.Id, e.Message));
    }

    private static bool HandleExit(McpSession session, ExitHandler exitHandler) => exitHandler.Handle(session);

    private static async ValueTask<JsonRpcResponse> HandleToolCallAsync(
        JsonRpcRequest request,
        McpSession session,
        ToolCallRouter router,
        CancellationToken ct)
    {
        var readiness = EnsureReady(request, session);
        if (readiness is not null)
        {
            return readiness;
        }

        var payload = request.Params?.Deserialize<CallToolRequestParams>();
        if (payload is null)
        {
            return JsonRpcErrorFactory.InvalidParams(request.Id, "Invalid tools/call params");
        }

        var result = await router.RouteAsync(payload.Name, payload.Arguments, ct).ConfigureAwait(false);

        return result.Match(
            Succ: x => new JsonRpcResponse("2.0", request.Id, x),
            Fail: e => JsonRpcErrorFactory.ServerError(request.Id, e.Message));
    }

    private static async ValueTask<JsonRpcResponse> HandleResourceReadAsync(
        JsonRpcRequest request,
        McpSession session,
        ResourceReadRouter router,
        CancellationToken ct)
    {
        var readiness = EnsureReady(request, session);
        if (readiness is not null)
        {
            return readiness;
        }

        var payload = request.Params?.Deserialize<ReadResourceRequestParams>();
        if (payload is null)
        {
            return JsonRpcErrorFactory.InvalidParams(request.Id, "Invalid resources/read params");
        }

        var result = await router.RouteAsync(payload.Uri, ct).ConfigureAwait(false);

        return result.Match(
            Succ: x => new JsonRpcResponse("2.0", request.Id, x),
            Fail: e => JsonRpcErrorFactory.ServerError(request.Id, e.Message));
    }

    private static async ValueTask<JsonRpcResponse> HandlePromptGetAsync(
        JsonRpcRequest request,
        McpSession session,
        PromptRouter router,
        CancellationToken ct)
    {
        var readiness = EnsureReady(request, session);
        if (readiness is not null)
        {
            return readiness;
        }

        var payload = request.Params?.Deserialize<GetPromptRequestParams>();
        if (payload is null)
        {
            return JsonRpcErrorFactory.InvalidParams(request.Id, "Invalid prompts/get params");
        }

        var result = await router.GetAsync(payload.Name, payload.Arguments, ct).ConfigureAwait(false);

        return result.Match(
            Succ: x => new JsonRpcResponse("2.0", request.Id, x),
            Fail: e => JsonRpcErrorFactory.ServerError(request.Id, e.Message));
    }

    private static JsonRpcResponse RequireReady(
        JsonRpcRequest request,
        McpSession session,
        Func<JsonRpcResponse> next) =>
        EnsureReady(request, session) ?? next();

    private static JsonRpcResponse? EnsureReady(JsonRpcRequest request, McpSession session) =>
        session.IsReady ? null : JsonRpcErrorFactory.SessionNotReady(request.Id);

    private readonly record struct DispatchResult(JsonRpcResponse? Response, bool ShouldExit)
    {
        public static DispatchResult WithResponse(JsonRpcResponse? response) => new(response, false);
        public static DispatchResult Exit(bool shouldExit) => new(null, shouldExit);
    }
}
