using Compiler.Lexer.Tokenization.Models;
using Compiler.Meta.Interfaces;

namespace Compiler.Meta
{
    public readonly struct Field : IField
    {
        public Field(string name,
            in IType type,
            in Span span,
            in DeprecatedAttribute? deprecatedAttribute,
            in int constantValue)
        {
            Name = name;
            Type = type;
            Span = span;
            DeprecatedAttribute = deprecatedAttribute;
            ConstantValue = constantValue;
        }

        public string Name { get; }
        public IType Type { get; }
        public Span Span { get; }
        public DeprecatedAttribute? DeprecatedAttribute { get; }
        public int ConstantValue { get; }
    }
}
