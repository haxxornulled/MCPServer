using McpServer.Application.Abstractions.Mcp;

namespace McpServer.Application.Abstractions.Mcp
{
    public interface IToolRegistry
    {
        bool TryGetHandler(string name, out IToolHandler<object> handler);
        IReadOnlyList<string> GetAvailableTools();
    }
}