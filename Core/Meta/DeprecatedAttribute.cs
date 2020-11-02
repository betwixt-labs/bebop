using Core.Meta.Interfaces.Attributes;

namespace Core.Meta
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
