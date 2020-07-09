using System.Collections.Generic;

namespace Compiler.Meta.Interfaces
{
    /// <summary>
    /// Represents a structure that encapsulates a set of data that belong together as a logical unit.
    /// The data are the members of the <see cref="AggregateKind"/> which act as a blueprint during code generation.
    /// </summary>
    public interface IDefinition
    {
        /// <summary>
        /// The name of the current definition.
        /// </summary>
        public string Name { get; }
        /// <summary>
        ///     The line coordinate where the definition was found.
        /// </summary>
        public uint Line { get; }
        /// <summary>
        ///     The column coordinate where the definition begins.
        /// </summary>
        public uint Column { get; }
        /// <summary>
        ///  An identifier for the <see cref="AggregateKind"/> used by the current definition.
        /// </summary>
        public AggregateKind Kind { get; }
        /// <summary>
        /// Declares that a structure type is immutable. Only valid for <see cref="AggregateKind.Struct"/>
        /// </summary>
        public bool IsReadOnly { get; }
        /// <summary>
        /// A collection of all <see cref="IField"/> 
        /// </summary>
        public ICollection<IField> Fields { get; }
    }
}