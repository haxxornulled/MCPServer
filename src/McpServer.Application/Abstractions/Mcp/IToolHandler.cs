using System.Text.Json;
using LanguageExt;

namespace McpServer.Application.Abstractions.Mcp
{
    public interface IToolHandler
    {
        string Name { get; }
        string Description { get; }
        JsonElement GetInputSchema();
    }

    public interface IToolHandler<TRequest> : IToolHandler
    {
        ValueTask<Fin<CallToolResult>> Handle(TRequest request, CancellationToken ct);
    }

    public record CallToolResult(
        IReadOnlyList<ContentItem> Content,
        object? StructuredContent = null,
        bool IsError = false);

    public record ContentItem(
        string Type,
        string Text);
}
