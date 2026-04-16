using Autofac;
using Autofac.Extensions.DependencyInjection;
using McpServer.Host.DependencyInjection;
using McpServer.Host.Configuration;
using McpServer.Host.Transport.Stdio;
using McpServer.Infrastructure.Logging;
using Serilog;
using System.Net;

var builder = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .UseSerilog((context, services, configuration) =>
    {
        SerilogBootstrap.Configure(configuration, context.Configuration);
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<McpServerOptions>(
            context.Configuration.GetSection(McpServerOptions.SectionName));

        services.AddHostedService<StdioServerHostedService>();

        services.AddHttpClient("web-access")
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });

        services.AddHttpClient("web-search")
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
    })
    .ConfigureContainer<ContainerBuilder>((context, container) =>
    {
        container.RegisterModule(new AutofacRootModule(context.Configuration));
    });

await builder.Build().RunAsync().ConfigureAwait(false);
