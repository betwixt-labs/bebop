using Compiler.Meta.Interfaces;

namespace Compiler.Meta
{
    public readonly struct Field : IField
    {
        public Field(string name,
            in IType type,
            in uint line,
            in uint column,
            in DeprecatedAttribute? deprecatedAttribute,
            in int constantValue)
        {
            Name = name;
            Type = type;
            Line = line;
            Column = column;
            DeprecatedAttribute = deprecatedAttribute;
            ConstantValue = constantValue;
        }

        public string Name { get; }
        public IType Type { get; }
        public uint Line { get; }
        public uint Column { get; }
        public DeprecatedAttribute? DeprecatedAttribute { get; }
        public int ConstantValue { get; }
    }
}
