using Autofac;
using McpServer.Application.WebSearch;

namespace McpServer.Application.DependencyInjection
{
    public class WebSearchRateLimiterModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(new InMemoryWebSearchRateLimiter(30)) // 30 searches/minute
                   .As<IWebSearchRateLimiter>()
                   .SingleInstance();
        }
    }
}
