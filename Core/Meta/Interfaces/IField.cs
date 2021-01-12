using Core.Lexer.Tokenization.Models;
using Core.Meta.Attributes;

namespace Core.Meta.Interfaces
{
    /// <summary>
    ///     A field represents any type that is declared directly in a <see cref="AggregateKind"/>.
    ///     Fields are members of their containing type.
    /// </summary>
    public interface IField
    {
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
        ///     A literal value associated with the member.
        /// </summary>
        /// <remarks>
        ///     For an <see cref="AggregateKind.Enum"/> this value corresponds to a defined member, and for
        ///     <see cref="AggregateKind.Message"/> a unique index.
        ///     It will be zero for <see cref="AggregateKind.Struct"/>.
        /// </remarks>
        public uint ConstantValue { get; }

        /// <summary>
        /// The inner text of a block comment that preceded the field.
        /// </summary>
        public string Documentation { get; }
    }

    public static class FieldExtensions
    {
        public static bool IsCollection(this IField field) => field.Type is ArrayType || field.Type is MapType;
    }
}