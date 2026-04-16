using LanguageExt;
using McpServer.Application.Abstractions.Web;
using McpServer.Application.Mcp.Tools;
using McpServer.Application.Web.Results;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace McpServer.UnitTests.Application;

public sealed class WebFetchToolHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Text_Content()
    {
        var web = Substitute.For<IWebAccessService>();
        var logger = Substitute.For<ILogger<WebFetchToolHandler>>();

        web.FetchUrlAsync(Arg.Any<McpServer.Application.Web.Commands.FetchUrlCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Fin<FetchedPageResult>>(new FetchedPageResult(
                Url: "https://example.com",
                StatusCode: 200,
                ContentType: "text/html",
                Title: "Example",
                Text: "Hello world",
                RawBody: null,
                Links: Array.Empty<string>())));

        var handler = new WebFetchToolHandler(web, logger);
        var result = await handler.Handle(new WebFetchUrlRequest("https://example.com"), CancellationToken.None);

        Assert.True(result.IsSucc);
    }
}
