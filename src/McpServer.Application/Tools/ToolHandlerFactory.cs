using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Application.Tools
{
    public static class ToolHandlerFactory
    {
        public static IToolHandler<TRequest> CreateHandler<TRequest>(IServiceProvider serviceProvider)
        {
            var handlerType = ToolHandlerRegistry.GetHandlerType(typeof(TRequest));
            
            if (handlerType == null)
                throw new InvalidOperationException($"No handler registered for request type: {typeof(TRequest).Name}");

            return (IToolHandler<TRequest>)serviceProvider.GetRequiredService(handlerType);
        }
    }
}