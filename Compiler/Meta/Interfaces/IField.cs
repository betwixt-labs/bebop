using Compiler.Lexer.Tokenization.Models;

namespace Compiler.Meta.Interfaces
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
        public IType Type { get; }

        /// <summary>
        ///     The span where the member was found.
        /// </summary>
        public Span Span { get; }

        /// <summary>
        ///     Indicates if the member has been marked as no longer recommended for use.
        /// </summary>
        public DeprecatedAttribute? DeprecatedAttribute { get; }

        /// <summary>
        ///     A literal value associated with the member.
        /// </summary>
        /// <remarks>
        ///     For an <see cref="AggregateKind.Enum"/> this value corresponds to a defined member, and for
        ///     <see cref="AggregateKind.Message"/> a unique index.
        ///     It will be zero for <see cref="AggregateKind.Struct"/>.
        /// </remarks>
        public int ConstantValue { get; }

        /// <summary>
        /// The inner text of a block comment that preceded the field.
        /// </summary>
        public string Documentation { get; }
    }
}