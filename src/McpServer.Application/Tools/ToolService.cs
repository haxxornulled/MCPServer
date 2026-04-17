using LanguageExt;
using McpServer.Application.Abstractions;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Tools
{
    public class ToolService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ToolService> _logger;

        public ToolService(IServiceProvider serviceProvider, ILogger<ToolService> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask<Fin<Unit>> ExecuteToolAsync(object request, CancellationToken ct)
        {
            var requestType = request.GetType();
            
            if (!ToolHandlerRegistry.IsRegistered(requestType))
            {
                _logger.LogError("No handler registered for tool request type: {RequestType}", requestType.Name);
                return Fin<Unit>.Fail(new Error($"No handler registered for tool: {requestType.Name}"));
            }

            try
            {
                var handler = ToolHandlerFactory.CreateHandler<object>(_serviceProvider);
                
                // This is a bit tricky since we need to cast to the specific type
                // We'll use reflection to invoke the appropriate method
                var method = typeof(ToolService).GetMethods()
                    .First(m => m.Name == nameof(ExecuteToolAsync) && m.IsGenericMethod)
                    .MakeGenericMethod(requestType);
                
                var result = await (ValueTask<Fin<Unit>>)method.Invoke(this, new object[] { request, ct })!;
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute tool: {RequestType}", requestType.Name);
                return Fin<Unit>.Fail(new Error($"Failed to execute tool: {ex.Message}"));
            }
        }

        public async ValueTask<Fin<Unit>> ExecuteToolAsync<TRequest>(TRequest request, CancellationToken ct) 
            where TRequest : class
        {
            var handlerType = ToolHandlerRegistry.GetHandlerType(typeof(TRequest));
            
            if (handlerType == null)
            {
                _logger.LogError("No handler registered for tool request type: {RequestType}", typeof(TRequest).Name);
                return Fin<Unit>.Fail(new Error($"No handler registered for tool: {typeof(TRequest).Name}"));
            }

            try
            {
                var handler = (IToolHandler<TRequest>)_serviceProvider.GetRequiredService(handlerType);
                var result = await handler.HandleAsync(request, ct);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute tool: {RequestType}", typeof(TRequest).Name);
                return Fin<Unit>.Fail(new Error($"Failed to execute tool: {ex.Message}"));
            }
        }
    }
}