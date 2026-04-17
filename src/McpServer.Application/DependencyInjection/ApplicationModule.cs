using Autofac;
using McpServer.Application.Mcp.Tools;

namespace McpServer.Application.DependencyInjection
{
    public class ApplicationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Register all tool handlers
            builder.RegisterAssemblyTypes(typeof(FsReadTextToolHandler).Assembly)
                .AssignableTo<IToolHandler>()
                .As<IToolHandler>()
                .InstancePerLifetimeScope();
        }
    }
}