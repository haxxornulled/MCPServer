using System.Text.Json;
using LanguageExt;
using LanguageExt.Common;
using McpServer.Application.Abstractions.Mcp;
using McpServer.Contracts.Prompts;

namespace McpServer.Application.Mcp.Prompts;

public sealed class ReviewDirectoryPromptHandler : IPromptHandler
{
    public string Name => "prompt.review_directory";
    public string Description => "Builds a prompt that asks the host model to review a directory resource.";

    public PromptDescriptor Describe() =>
        new(
            Name: Name,
            Title: "Review directory",
            Description: Description,
            Arguments:
            [
                new PromptArgumentDescriptor("uri", "Directory resource URI", "A dir:// resource URI to review.", true),
                new PromptArgumentDescriptor("goal", "Goal", "Optional review goal.", false)
            ]);

    public ValueTask<Fin<GetPromptResult>> GetAsync(JsonElement? arguments, CancellationToken ct)
    {
        ReviewDirectoryPromptArguments? request = null;

        if (arguments.HasValue && arguments.Value.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            request = arguments.Value.Deserialize<ReviewDirectoryPromptArguments>();
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Uri))
        {
            return ValueTask.FromResult<Fin<GetPromptResult>>(Error.New("Prompt 'prompt.review_directory' requires argument 'uri'."));
        }

        var goalClause = string.IsNullOrWhiteSpace(request.Goal)
            ? "Review the directory for structure, important files, possible concerns, and suggested next steps."
            : $"Review the directory with this goal in mind: {request.Goal}.";

        var result = new GetPromptResult(
            Description: "Prompt for reviewing a directory resource.",
            Messages:
            [
                new PromptMessage(
                    Role: "user",
                    Content: PromptMessageContent.FromText(
                        $"Please inspect the resource at '{request.Uri}'. {goalClause}"))
            ]);

        return ValueTask.FromResult<Fin<GetPromptResult>>(result);
    }
}
