using LanguageExt;
using System.Text.Json;

namespace McpServer.Application.Abstractions.Mcp;

public interface IToolHandler<in TRequest>
{
    string Name { get; }
    string Description { get; }
    JsonElement GetInputSchema();
    ValueTask<Fin<CallToolResult>> Handle(TRequest request, CancellationToken ct);
}

public sealed record CallToolResult(
    IReadOnlyList<ContentItem> Content,
    object? StructuredContent = null,
    bool? IsError = null);

public sealed record ContentItem(string Type, string Text);
