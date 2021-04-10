using System.Collections.Generic;
using Core.Lexer.Tokenization.Models;

namespace Core.Meta.Interfaces
{
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
}
