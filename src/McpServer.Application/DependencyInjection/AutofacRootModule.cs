using Autofac;
using McpServer.Application.Abstractions.Files;
using McpServer.Application.Abstractions.Ssh;
using McpServer.Application.Abstractions.Web;
using McpServer.Application.Abstractions.Execution;
using McpServer.Application.Files;
using McpServer.Application.Ssh;
using McpServer.Application.Web;
using McpServer.Application.Execution;
using McpServer.Host.Configuration;

namespace McpServer.Application.DependencyInjection
{
    public class AutofacRootModule : Module
    {
        private readonly IConfiguration _configuration;

        public AutofacRootModule(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        protected override void Load(ContainerBuilder builder)
        {
            // Register infrastructure modules
            builder.RegisterModule<InfrastructureModule>();
            
            // Register services with proper lifetimes
            builder.RegisterType<FileSystemService>()
                .As<IFileSystemService>()
                .WithParameter(
                    (pi, ctx) => pi.ParameterType == typeof(string),
                    (pi, ctx) => _configuration.GetSection(McpServerOptions.SectionName)
                        .Get<McpServerOptions>()?.Workspace.RootPath ?? "./workspace")
                .InstancePerLifetimeScope();

            builder.RegisterType<SshService>()
                .As<ISshService>()
                .WithParameter(
                    (pi, ctx) => pi.ParameterType == typeof(IEnumerable<SshProfileOptions>),
                    (pi, ctx) => _configuration.GetSection(McpServerOptions.SectionName)
                        .Get<McpServerOptions>()?.Ssh.Profiles ?? [])
                .InstancePerLifetimeScope();

            builder.RegisterType<WebAccessService>()
                .As<IWebAccessService>()
                .WithParameter(
                    (pi, ctx) => pi.ParameterType == typeof(string),
                    (pi, ctx) => _configuration.GetSection("WebSearch:BaseUrl")?.Value ?? "https://your-search-api")
                .WithParameter(
                    (pi, ctx) => pi.ParameterType == typeof(string),
                    (pi, ctx) => _configuration.GetSection("WebSearch:ApiKey")?.Value ?? "")
                .InstancePerLifetimeScope();

            builder.RegisterType<ProcessExecutionService>()
                .As<IProcessExecutionService>()
                .InstancePerLifetimeScope();
        }
    }
}