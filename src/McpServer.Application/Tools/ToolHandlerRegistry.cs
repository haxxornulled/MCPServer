using McpServer.Contracts.Tools;

namespace McpServer.Application.Tools
{
    public static class ToolHandlerRegistry
    {
        private static readonly Dictionary<Type, Type> _handlerMap = new()
        {
            { typeof(FsReadTextRequest), typeof(FsReadTextToolHandler) },
            { typeof(FsWriteTextRequest), typeof(FsWriteTextToolHandler) },
            { typeof(AppendFileTextRequest), typeof(FsAppendTextToolHandler) },
            { typeof(FsListDirectoryRequest), typeof(FsListDirectoryToolHandler) },
            { typeof(FsGetMetadataRequest), typeof(FsGetMetadataToolHandler) },
            { typeof(FsCreateDirectoryRequest), typeof(FsCreateDirectoryToolHandler) },
            { typeof(FsMovePathRequest), typeof(FsMovePathToolHandler) },
            { typeof(FsCopyPathRequest), typeof(FsCopyPathToolHandler) },
            { typeof(FsDeletePathRequest), typeof(FsDeletePathToolHandler) },
            { typeof(SshExecuteRequest), typeof(SshExecuteToolHandler) },
            { typeof(SshWriteTextRequest), typeof(SshWriteTextToolHandler) },
            { typeof(WebSearchRequest), typeof(WebSearchToolHandler) },
            { typeof(WebFetchUrlRequest), typeof(WebFetchUrlToolHandler) },
            { typeof(ExecRunProcessRequest), typeof(ExecRunProcessToolHandler) }
        };

        public static Type? GetHandlerType(Type requestType)
        {
            return _handlerMap.GetValueOrDefault(requestType);
        }

        public static bool IsRegistered(Type requestType)
        {
            return _handlerMap.ContainsKey(requestType);
        }
    }
}
