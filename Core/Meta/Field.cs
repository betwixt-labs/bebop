using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using Core.Lexer.Tokenization.Models;
using Core.Meta.Attributes;
using Core.Meta.Extensions;

namespace Core.Meta
{
    public readonly struct Field
    {
        public Field(string name,
            in TypeBase type,
            in Span span,
            in List<BaseAttribute>? attributes,
            in BigInteger constantValue, string documentation)
        {
            Name = name;
            Type = type;
            Span = span;
            Attributes = attributes;
            DeprecatedAttribute = attributes?.FirstOrDefault((a) => a is DeprecatedAttribute);
            ConstantValue = constantValue;
            Documentation = documentation;
        }

        /// <summary>
        ///     The name of the current member.
        /// </summary>
        public string Name { get; }
        public string NameCamelCase => Name.ToCamelCase();

        /// <summary>
        ///     The type of this field: a scalar, array, or defined type (enum/message/struct).
        /// </summary>
        public TypeBase Type { get; }

        /// <summary>
        ///     The span where the member was found.
        /// </summary>
        public Span Span { get; }

        public List<BaseAttribute>? Attributes {get; }

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
