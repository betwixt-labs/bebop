using System.Numerics;
using Core.Lexer.Tokenization.Models;
using Core.Meta.Attributes;

namespace Core.Meta
{
    public readonly struct Field
    {
        public Field(string name,
            in TypeBase type,
            in Span span,
            in BaseAttribute? deprecatedAttribute,
            in BigInteger constantValue, string documentation)
        {
            Name = name;
            Type = type;
            Span = span;
            DeprecatedAttribute = deprecatedAttribute;
            ConstantValue = constantValue;
            Documentation = documentation;
        }

        /// <summary>
        ///     The name of the current member.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     The type of this field: a scalar, array, or defined type (enum/message/struct).
        /// </summary>
        public TypeBase Type { get; }

        /// <summary>
        ///     The span where the member was found.
        /// </summary>
        public Span Span { get; }

        /// <summary>
        ///     Indicates if the member has been marked as no longer recommended for use.
        /// </summary>
        public BaseAttribute? DeprecatedAttribute { get; }

        /// <summary>
        /// For enums, this is a constant value. For messages, this is a field index. For structs, this is unused.
        /// </summary>
        public BigInteger ConstantValue { get; }

        /// <summary>
        /// The inner text of a block comment that preceded the field.
        /// </summary>
        public string Documentation { get; }

        public bool IsCollection() => Type is ArrayType or MapType;

        public int MinimalEncodedSize(BebopSchema schema) => Type.MinimalEncodedSize(schema);
    }
}
