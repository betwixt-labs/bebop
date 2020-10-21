using System.Collections.Generic;

namespace Compiler.Meta.Interfaces
{
    /// <summary>
    /// Represents the contents of a textual Pierogi schema 
    /// </summary>
    public interface ISchema
    { 
        /// <summary>
        /// A path to the schema on disk.
        /// </summary>
        public string SourcePath { get; }

        /// <summary>
        /// A collection of data structures defined in the schema
        /// </summary>
        public Dictionary<string, IDefinition> Definitions { get; }
        /// <summary>
        /// Validates that the schema is made up of well-formed values.
        /// </summary>
        public void Validate();

    }
}
