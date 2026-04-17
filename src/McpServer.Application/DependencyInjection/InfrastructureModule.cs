using Autofac;
using McpServer.Application.Abstractions.Execution;
using McpServer.Application.Abstractions.Files;
using McpServer.Application.Abstractions.Ssh;
using McpServer.Application.Abstractions.Web;
using McpServer.Application.Execution;
using McpServer.Application.Files;
using McpServer.Application.Ssh;
using McpServer.Application.Web;

namespace McpServer.Application.DependencyInjection
{
    public class InfrastructureModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // File system services
            builder.RegisterType<FileSystemService>()
                .As<IFileSystemService>()
                .InstancePerLifetimeScope();

            // SSH services
            builder.RegisterType<SshService>()
                .As<ISshService>()
                .InstancePerLifetimeScope();

            // Web access services
            builder.RegisterType<WebAccessService>()
                .As<IWebAccessService>()
                .InstancePerLifetimeScope();

            // Process execution services
            builder.RegisterType<ProcessExecutionService>()
                .As<IProcessExecutionService>()
                .InstancePerLifetimeScope();
        }
    }
}