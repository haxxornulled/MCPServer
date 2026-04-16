using LanguageExt;
using McpServer.Application.Web.Commands;
using McpServer.Application.Web.Results;

namespace McpServer.Application.Abstractions.Web;

public interface IWebAccessService
{
    ValueTask<Fin<FetchedPageResult>> FetchUrlAsync(FetchUrlCommand command, CancellationToken ct);
    ValueTask<Fin<IReadOnlyList<SearchHitResult>>> SearchWebAsync(SearchWebCommand command, CancellationToken ct);
}
