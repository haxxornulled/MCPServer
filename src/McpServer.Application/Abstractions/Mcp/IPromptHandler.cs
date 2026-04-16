using LanguageExt;
using System.Text.Json;

namespace McpServer.Application.Abstractions.Mcp;

public interface IPromptHandler
{
    string Name { get; }
    string Description { get; }
    PromptDescriptor Describe();
    ValueTask<Fin<GetPromptResult>> GetAsync(JsonElement? arguments, CancellationToken ct);
}

public sealed record PromptDescriptor(
    string Name,
    string? Title,
    string? Description,
    IReadOnlyList<PromptArgumentDescriptor>? Arguments);

public sealed record PromptArgumentDescriptor(
    string Name,
    string? Title,
    string? Description,
    bool Required);

public sealed record GetPromptResult(
    string? Description,
    IReadOnlyList<PromptMessage> Messages);

public sealed record PromptMessage(string Role, PromptMessageContent Content);

public sealed record PromptMessageContent(string Type, string Text)
{
    public static PromptMessageContent FromText(string text) => new("text", text);
}
