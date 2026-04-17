using LanguageExt;

namespace McpServer.Application.Abstractions
{
    public class Fin<T> : Fin<T, IError>
    {
        public Fin(T value) : base(value)
        {
        }

        public Fin(IError error) : base(error)
        {
        }
    }
}