namespace McpServer.Application.Abstractions
{
    public record Error(string Message) : IError;
}