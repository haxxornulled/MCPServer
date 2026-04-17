
using Autofac;
using McpServer.Application.Mcp.Tools;
using McpServer.Application.Abstractions.Mcp;
using McpServer.Contracts.Tools;
using McpServer.Application.WebSearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace McpServer.Application.DependencyInjection
{
    public class WebSearchModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<WebSearchToolHandler>().As<IToolHandler<WebSearchRequest>>();

            builder.Register((ctx, p) =>
            {
                var config = ctx.Resolve<IConfiguration>();
                var logger = ctx.Resolve<ILogger<CustomWebSearchApi>>();
                var httpClient = ctx.Resolve<HttpClient>();
                var baseUrl = config["WebSearch:BaseUrl"] ?? "https://your-search-api";
                var apiKey = config["WebSearch:ApiKey"] ?? "";
                return new CustomWebSearchApi(httpClient, logger, baseUrl, apiKey);
            }).As<ICustomWebSearchApi>().SingleInstance();
        }
    }
}
