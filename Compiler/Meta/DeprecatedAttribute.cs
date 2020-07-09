using Compiler.Meta.Interfaces.Attributes;

namespace Compiler.Meta
{
    public readonly struct DeprecatedAttribute : IAttribute
    {
        public DeprecatedAttribute(string message)
        {
            Message = message;
        }
        public string Message { get; }
        public string Name => "Deprecated";
    }
}
