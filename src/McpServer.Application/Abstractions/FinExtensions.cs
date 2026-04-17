using LanguageExt;

namespace McpServer.Application.Abstractions
{
    public static class FinExtensions
    {
        public static Fin<T> Succ<T>(T value)
        {
            return new Fin<T>(value);
        }

        public static Fin<T> Fail<T>(IError error)
        {
            return new Fin<T>(error);
        }

        public static Fin<T> Fail<T>(string message)
        {
            return new Fin<T>(new Error(message));
        }
    }
}