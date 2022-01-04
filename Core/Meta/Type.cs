using System;
using System.Collections.Generic;
using System.Linq;
using Core.Lexer.Tokenization.Models;

namespace Core.Meta
{
    /// <summary>
    /// An abstract base class for Bebop schema types.
    /// </summary>
    public abstract class TypeBase
    {
        protected TypeBase(Span span, string asString)
        {
            Span = span;
            AsString = asString;
        }

        /// <summary>
        ///     The span where the type was parsed.
        /// </summary>
        public Span Span { get; }

        /// <summary>
        ///     A string used to display this type's name in error messages.
        /// </summary>
        public string AsString { get; }

        public override string? ToString() => AsString;

        /// <summary>
        /// The names of types this definition depends on / refers to.
        /// </summary>
        internal abstract IEnumerable<string> Dependencies();
    }

    /// <summary>
    /// A scalar type, like "int" or "byte". It represents *one* of its underlying base type.
    /// </summary>
    public class ScalarType : TypeBase
    {
        public BaseType BaseType { get; }

        public ScalarType(BaseType baseType, Span span, string asString) : base(span, asString)
        {
            BaseType = baseType;
        }

        public ScalarType(BaseType baseType) : this(baseType, new Span(), "")
        {
        }

        internal override IEnumerable<string> Dependencies() => Enumerable.Empty<string>();

        public bool IsSignedInteger => BaseType == BaseType.Int16 || BaseType == BaseType.Int32 || BaseType == BaseType.Int64;
        public bool IsUnsignedInteger => BaseType == BaseType.Byte || BaseType == BaseType.UInt16 || BaseType == BaseType.UInt32 || BaseType == BaseType.UInt64;
        public bool IsFloat => BaseType == BaseType.Float32 || BaseType == BaseType.Float64;
        public bool IsInteger => IsSignedInteger || IsUnsignedInteger;
        public bool Is64Bit => BaseType == BaseType.Int64 || BaseType == BaseType.UInt64;
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

        public bool IsOneByteUnits()
        {
            return MemberType is ScalarType st && (st.BaseType == BaseType.Bool || st.BaseType == BaseType.Byte);
        }

        internal override IEnumerable<string> Dependencies() => MemberType.Dependencies();
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

        internal override IEnumerable<string> Dependencies() => KeyType.Dependencies().Concat(ValueType.Dependencies());
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

        internal override IEnumerable<string> Dependencies() => new[] { Name };
    }

    public static class TypeExtensions
    {
        public static bool IsFixedScalar(this TypeBase type)
        {
            return type is ScalarType and { BaseType: not BaseType.String };
        }
        public static bool IsStruct(this TypeBase type, BebopSchema schema)
        {
            return type is DefinedType dt && schema.Definitions[dt.Name] is StructDefinition;
        }

        public static bool IsMessage(this TypeBase type, BebopSchema schema)
        {
            return type is DefinedType dt && schema.Definitions[dt.Name] is MessageDefinition;
        }

        public static bool IsUnion(this TypeBase type, BebopSchema schema)
        {
            return type is DefinedType dt && schema.Definitions[dt.Name] is UnionDefinition;
        }

        public static bool IsEnum(this TypeBase type, BebopSchema schema)
        {
            return type is DefinedType dt && schema.Definitions[dt.Name] is EnumDefinition;
        }

        public static int MinimalEncodedSize(this TypeBase type, BebopSchema schema)
        {
            return type switch
            {
                ArrayType or MapType => 4,
                ScalarType st => st.BaseType.Size(),
                DefinedType dt when schema.Definitions[dt.Name] is EnumDefinition => 4,
                DefinedType dt when schema.Definitions[dt.Name] is TopLevelDefinition td => td.MinimalEncodedSize(schema),
                _ => throw new ArgumentOutOfRangeException(type.ToString())
            };
        }

        public static int Size(this BaseType type)
        {
            return type switch
            {
                BaseType.Bool => 1,
                BaseType.Byte => 1,
                BaseType.UInt16 or BaseType.Int16 => 2,
                BaseType.UInt32 or BaseType.Int32 => 4,
                BaseType.UInt64 or BaseType.Int64 => 8,
                BaseType.Float32 => 4,
                BaseType.Float64 => 8,
                BaseType.String => 4,
                BaseType.Guid => 16,
                BaseType.Date => 8,
                _ => throw new ArgumentOutOfRangeException(type.ToString()),
            };
        }
    }
}
