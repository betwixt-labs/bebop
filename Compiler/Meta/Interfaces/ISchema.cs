using System.Collections.Generic;

namespace Compiler.Meta.Interfaces
{
    /// <summary>
    /// Represents the contents of a textual Pierogi schema 
    /// </summary>
    public interface ISchema
    {
        /// <summary>
        /// An optional package specifier within the .pie file to prevent name clashes between types.
        /// </summary>
        public string Package { get; }
        /// <summary>
        /// A collection of data structures defined in the schema
        /// </summary>
        public ICollection<IDefinition> Definitions { get; }

    }
}
