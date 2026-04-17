using LanguageExt;
using McpServer.Application.Abstractions;

namespace McpServer.Application.Tools
{
    public interface IToolHandler<TRequest>
    {
        ValueTask<Fin<Unit>> HandleAsync(TRequest request, CancellationToken ct);
    }
}