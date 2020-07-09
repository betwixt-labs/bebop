using Compiler.Meta.Interfaces;

namespace Compiler.Meta
{
    public readonly struct Field : IField
    {
        public Field(string name,
            in int type,
            in uint line,
            in uint column,
            in bool isArray,
            in DeprecatedAttribute? deprecatedAttribute,
            in int constantValue)
        {
            Name = name;
            TypeCode = type;
            Line = line;
            Column = column;
            IsArray = isArray;
            DeprecatedAttribute = deprecatedAttribute;
            ConstantValue = constantValue;
        }

        public string Name { get; }
        public int TypeCode { get; }
        public uint Line { get; }
        public uint Column { get; }
        public bool IsArray { get; }
        public DeprecatedAttribute? DeprecatedAttribute { get; }
        public int ConstantValue { get; }
    }
}
