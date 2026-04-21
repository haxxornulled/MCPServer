using Autofac;
using McpServer.Application.Abstractions.Files;
using McpServer.Application.Abstractions.Mcp;
using McpServer.Application.Abstractions.Web;
using McpServer.Application.Abstractions.Execution;
using McpServer.Application.Abstractions.Ssh;
using McpServer.Application.Mcp.Prompts;
using McpServer.Application.Mcp.Resources;
using McpServer.Application.Mcp.Tools;
using McpServer.Infrastructure.Execution;
using McpServer.Host.Configuration;
using McpServer.Infrastructure.Files;
using McpServer.Infrastructure.Ssh;
using McpServer.Infrastructure.Web;
using McpServer.Protocol.Lifecycle;
using McpServer.Protocol.Routing;
using McpServer.Protocol.Session;

namespace McpServer.Host.DependencyInjection;

public sealed class AutofacRootModule : Module
{
    private readonly IConfiguration _configuration;

    public AutofacRootModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        var options = _configuration.GetSection(McpServerOptions.SectionName).Get<McpServerOptions>() ?? new McpServerOptions();
        var workspace = ResolveWorkspacePath(options.Workspace.RootPath);

        Directory.CreateDirectory(workspace);

        builder.RegisterInstance(options).AsSelf().SingleInstance();

        builder.RegisterType<McpSession>().SingleInstance();
        builder.RegisterType<CapabilityProvider>().SingleInstance();
        builder.RegisterType<InitializeHandler>().SingleInstance();
        builder.RegisterType<ShutdownHandler>().SingleInstance();
        builder.RegisterType<ExitHandler>().SingleInstance();
        builder.RegisterType<ToolCallRouter>().SingleInstance();
        builder.RegisterType<ResourceReadRouter>().SingleInstance();
        builder.RegisterType<PromptRouter>().SingleInstance();

        builder.RegisterType<FileMutationLockProvider>()
            .As<IFileMutationLockProvider>()
            .SingleInstance();

        builder.Register(_ => new ResourcePathTranslator(workspace))
            .AsSelf()
            .As<IResourcePathTranslator>()
            .SingleInstance();

        builder.Register(_ => new PathPolicy([workspace]))
            .AsSelf()
            .As<IPathPolicy>()
            .SingleInstance();

        builder.RegisterType<FileSystemService>()
            .As<IFileSystemService>()
            .SingleInstance();

        builder.RegisterType<ProcessExecutionService>()
            .As<IProcessExecutionService>()
            .SingleInstance();


        builder.RegisterType<FsWriteTextToolHandler>().AsSelf().SingleInstance();
        builder.RegisterType<FsAppendTextToolHandler>().AsSelf().SingleInstance();
        builder.RegisterType<FsReadFileToolHandler>().AsSelf().SingleInstance();
        builder.RegisterType<FsCreateDirectoryToolHandler>().AsSelf().SingleInstance();
        builder.RegisterType<FsMovePathToolHandler>().AsSelf().SingleInstance();
        builder.RegisterType<FsCopyPathToolHandler>().AsSelf().SingleInstance();
        builder.RegisterType<FsDeletePathToolHandler>().AsSelf().SingleInstance();
        builder.RegisterType<ShellExecToolHandler>().AsSelf().SingleInstance();

        builder.RegisterType<FsFileTextResourceHandler>().As<IResourceHandler>().SingleInstance();
        builder.RegisterType<FsDirectoryResourceHandler>().As<IResourceHandler>().SingleInstance();
        builder.RegisterType<FsFileMetadataResourceHandler>().As<IResourceHandler>().SingleInstance();

        builder.RegisterType<SummarizeFilePromptHandler>().As<IPromptHandler>().SingleInstance();
        builder.RegisterType<ReviewDirectoryPromptHandler>().As<IPromptHandler>().SingleInstance();

        if (options.WebAccess.Enabled)
        {
            builder.Register(ctx =>
                {
                    var allowedHosts = options.WebAccess.AllowedHosts
                        .Where(static x => !string.IsNullOrWhiteSpace(x))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    return new WebPolicy(allowedHosts);
                })
                .As<IWebPolicy>()
                .SingleInstance();

            builder.RegisterType<WebAccessService>().As<IWebAccessService>().SingleInstance();
            builder.RegisterType<WebFetchUrlToolHandler>().AsSelf().SingleInstance();
            builder.RegisterType<WebSearchToolHandler>().AsSelf().SingleInstance();
        }

        if (options.Ssh.Enabled && options.Ssh.Profiles.Length > 0)
        {
            builder.Register(ctx => new SshService(
                    CreateConfiguredProfiles(options.Ssh.Profiles),
                    AppContext.BaseDirectory,
                    ctx.Resolve<ILogger<SshService>>()))
                .As<ISshService>()
                .SingleInstance();

            builder.RegisterType<SshExecuteToolHandler>().AsSelf().SingleInstance();
            builder.RegisterType<SshWriteTextToolHandler>().AsSelf().SingleInstance();
        }
    }

    private static string ResolveWorkspacePath(string rootPath)
    {
        if (Path.IsPathRooted(rootPath))
        {
            return Path.GetFullPath(rootPath);
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, rootPath));
    }

    private static ConfiguredSshProfile[] CreateConfiguredProfiles(IEnumerable<SshProfileOptions> profiles) =>
        profiles
            .Where(static profile => !string.IsNullOrWhiteSpace(profile.Name))
            .Select(static profile => new ConfiguredSshProfile(
                profile.Name,
                profile.Host,
                profile.Port,
                profile.Username,
                profile.PasswordEnvironmentVariable,
                profile.PrivateKeyPath,
                profile.PrivateKeyPassphraseEnvironmentVariable,
                profile.WorkingDirectory,
                profile.HostKeySha256,
                profile.AcceptUnknownHostKey))
            .ToArray();
}
