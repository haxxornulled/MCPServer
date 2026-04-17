using Autofac;
using McpServer.Application.WebSearch;

namespace McpServer.Application.DependencyInjection
{
    public class WebSearchAuditLoggerModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ConsoleWebSearchAuditLogger>()
                   .As<IWebSearchAuditLogger>()
                   .SingleInstance();
        }
    }
}
