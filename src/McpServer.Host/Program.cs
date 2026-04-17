
using Autofac;
using Autofac.Extensions.DependencyInjection;
using McpServer.Host.DependencyInjection;
using McpServer.Host.Configuration;
using McpServer.Host.Transport.Stdio;
using McpServer.Infrastructure.Logging;
using Serilog;
using System.Net;
using VapeCache.Extensions.DependencyInjection;
using VapeCache.Abstractions.Caching;
// removed unused using to satisfy IDE analyzers

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

        // Register VapeCache hybrid runtime when Redis configured, otherwise use in-memory for local/dev
        var redisSection = context.Configuration.GetSection("RedisConnection");
        // Consider explicit environment override used by VapeCache (VAPECACHE_REDIS_CONNECTIONSTRING)
        var envConnStr = Environment.GetEnvironmentVariable("VAPECACHE_REDIS_CONNECTIONSTRING");
        var redisConfigured = false;

        if (!string.IsNullOrWhiteSpace(envConnStr))
        {
            redisConfigured = true;
        }
        else if (redisSection.Exists())
        {
            var host = redisSection["Host"];
            var cs = redisSection["ConnectionString"];
            if (!string.IsNullOrWhiteSpace(host) || !string.IsNullOrWhiteSpace(cs))
            {
                redisConfigured = true;
            }
        }

        if (redisConfigured)
        {
            Console.Error.WriteLine("[Startup] Redis configuration present; registering VapeCache hybrid runtime.");
            services.AddVapeCache(context.Configuration);
        }
        else
        {
            Console.Error.WriteLine("[Startup] Redis not configured; registering VapeCache in-memory fallback.");
            services.AddVapeCacheInMemory(context.Configuration)
                    .WithCacheStampedeProfile(CacheStampedeProfile.Balanced);
        }
    })
    .ConfigureContainer<ContainerBuilder>((context, container) =>
    {
        container.RegisterModule(new AutofacRootModule(context.Configuration));
    });

await builder.Build().RunAsync().ConfigureAwait(false);
