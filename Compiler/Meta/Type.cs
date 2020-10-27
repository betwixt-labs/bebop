using Compiler.Lexer.Tokenization.Models;
using Compiler.Meta.Interfaces;

namespace Compiler.Meta
{
    /// <summary>
    /// A scalar type, like "int" or "byte". It represents *one* of its underlying base type.
    /// </summary>
    class ScalarType : TypeBase
    {
        public BaseType BaseType { get; }

        public ScalarType(BaseType baseType, Span span, string asString) : base(span, asString)
        {
            BaseType = baseType;
        }
    }

    /// <summary>
    /// An array type, like "int[]" or "Foo[]". It represents an array of its underlying type.
    /// </summary>
    class ArrayType : TypeBase
    {
        public TypeBase MemberType { get; }
        public ArrayType(TypeBase memberType, Span span, string asString) : base(span, asString)
        {
            MemberType = memberType;
        }

        public bool IsBytes()
        {
            return MemberType is ScalarType st && st.BaseType == BaseType.Byte;
        }

        public bool IsFloat32s()
        {
            return MemberType is ScalarType st && st.BaseType == BaseType.Float32;
        }

        public bool IsFloat64s()
        {
            return MemberType is ScalarType st && st.BaseType == BaseType.Float64;
        }
    }

    /// <summary>
    /// A map type, like "map[int, int]" or "map[string, map[int, Foo]]".
    /// It represents a list of key-value pairs, where each key occurs only once.
    /// </summary>
    class MapType : TypeBase
    {
        public TypeBase KeyType { get; }
        public TypeBase ValueType { get; }
        public MapType(TypeBase keyType, TypeBase valueType, Span span, string asString) : base(span, asString)
        {
            KeyType = keyType;
            ValueType = valueType;
        }
    }

    class DefinedType : TypeBase
    {
        /// <summary>
        /// The name of the defined type being referred to.
        /// </summary>
        public string Name { get; }

        public DefinedType(string name, Span span, string asString) : base(span, asString)
        {
            Name = name;
        }
    }
}
