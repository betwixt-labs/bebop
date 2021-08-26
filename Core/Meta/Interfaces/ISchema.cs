using System.Collections.Generic;
using Core.Exceptions;

namespace Core.Meta.Interfaces
{
    /// <summary>
    /// Represents the contents of a textual Bebop schema 
    /// </summary>
    public interface ISchema
    {
        /// <summary>
        /// Errors found while validating this schema.
        /// </summary>
        public List<SpanException> Errors { get; }
        /// <summary>
        /// An optional namespace that is provided to the compiler.
        /// </summary>
        public string Namespace { get; }
        /// <summary>
        /// A collection of data structures defined in the schema
        /// </summary>
        public Dictionary<string, Definition> Definitions { get; }
        /// <summary>
        /// Validates that the schema is made up of well-formed values.
        /// </summary>
        public List<SpanException> Validate();
        /// <summary>
        /// A topologically sorted list of definitions.
        /// </summary>
        public List<Definition> SortedDefinitions();

    }
}
