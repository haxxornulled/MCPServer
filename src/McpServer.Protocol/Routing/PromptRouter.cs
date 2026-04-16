using System.Text.Json;
using LanguageExt;
using LanguageExt.Common;
using McpServer.Application.Abstractions.Mcp;
using McpServer.Contracts.Prompts;

namespace McpServer.Protocol.Routing;

public sealed class PromptRouter(IEnumerable<IPromptHandler> handlers)
{
    private readonly IReadOnlyDictionary<string, IPromptHandler> _byName =
        handlers.ToDictionary(x => x.Name, StringComparer.Ordinal);

    public ListPromptsResult ListPrompts() =>
        new(
            Prompts: _byName.Values
                .Select(x => x.Describe())
                .Select(d => new PromptDto(
                    Name: d.Name,
                    Title: d.Title,
                    Description: d.Description,
                    Arguments: d.Arguments?.Select(a => new PromptArgumentDto(
                        Name: a.Name,
                        Title: a.Title,
                        Description: a.Description,
                        Required: a.Required)).ToArray()))
                .ToArray(),
            NextCursor: null);

    public async ValueTask<Fin<GetPromptResultDto>> GetAsync(string name, JsonElement? arguments, CancellationToken ct)
    {
        if (!_byName.TryGetValue(name, out var handler))
        {
            return Error.New($"Unknown prompt: {name}");
        }

        var result = await handler.GetAsync(arguments, ct).ConfigureAwait(false);
        return result.Map(ToDto);
    }

    private static GetPromptResultDto ToDto(GetPromptResult result) =>
        new(
            Description: result.Description,
            Messages: result.Messages
                .Select(m => new PromptMessageDto(
                    Role: m.Role,
                    Content: PromptMessageContentDto.FromText(m.Content.Text)))
                .ToArray());
}
