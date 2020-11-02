using System.Collections.Generic;
using Core.Lexer.Tokenization.Models;

namespace Core.Meta.Interfaces
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
        ///     The span where the definition was found.
        /// </summary>
        public Span Span { get; }
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
        /// <summary>
        /// The inner text of a block comment that preceded the definition.
        /// </summary>
        public string Documentation { get; }
    }

    public static class DefinitionExtensions
    {
        public static bool IsEnum(this IDefinition definition) => definition.Kind == AggregateKind.Enum;
        public static bool IsStruct(this IDefinition definition) => definition.Kind == AggregateKind.Struct;
        public static bool IsMessage(this IDefinition definition) => definition.Kind == AggregateKind.Message;
    }
}