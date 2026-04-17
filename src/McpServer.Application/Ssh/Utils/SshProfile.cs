namespace McpServer.Application.Ssh.Utils
{
    public record SshProfile(string Name, string Host, int Port, string Username, string? Password = null, string? KeyPath = null);
}