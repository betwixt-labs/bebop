using System.Collections.Generic;
using System.Runtime.Serialization;
using Core.Lexer.Tokenization.Models;
using Core.Meta.Attributes;
using Core.Meta.Interfaces;

namespace Core.Meta
{
    /// <summary>
    /// A base class for definitions in a schema.
    /// </summary>
    public abstract class Definition
    {
        protected Definition(string name, Span span, string documentation)
        {
            Name = name;
            Span = span;
            Documentation = documentation;
        }

        /// <summary>
        /// The name of the current definition.
        /// </summary>
        public string Name { get; }
        /// <summary>
        ///     The span where the definition was found.
        /// </summary>
        public Span Span { get; }
        /// <summary>
        /// The inner text of a block comment that preceded the definition.
        /// </summary>
        public string Documentation { get; }
    }

    /// <summary>
    /// A base class for definitions that can have an opcode, and are therefore valid at the "top level" of a Bebop packet.
    /// (In other words: struct, message, union. But you can't send a raw enum over the wire.)
    /// </summary>
    public abstract class TopLevelDefinition : Definition
    {
        protected TopLevelDefinition(string name, Span span, string documentation, BaseAttribute? opcodeAttribute) : base(name, span, documentation)
        {
            OpcodeAttribute = opcodeAttribute;
        }

        public BaseAttribute? OpcodeAttribute { get; }
    }

    /// <summary>
    /// A base class for definitions that are an aggregate of fields. (struct, message)
    /// </summary>
    public abstract class FieldsDefinition : TopLevelDefinition
    {
        protected FieldsDefinition(string name, Span span, string documentation, BaseAttribute? opcodeAttribute, ICollection<IField> fields) : base(name, span, documentation, opcodeAttribute)
        {
            Fields = fields;
        }

        public ICollection<IField> Fields { get; }
    }

    /// <summary>
    /// A class representing a struct definition.
    /// 
    /// A struct is an aggregate of some fields that are always present in a fixed order. It promises to never grow in later versions of the schema.
    /// </summary>
    public class StructDefinition : FieldsDefinition
    {
        public StructDefinition(string name, Span span, string documentation, BaseAttribute? opcodeAttribute, ICollection<IField> fields, bool isReadOnly) : base(name, span, documentation, opcodeAttribute, fields)
        {
            IsReadOnly = isReadOnly;
        }

        /// <summary>
        /// Is this struct "read-only"? (This will mean something like: not generating setters in the codegen.)
        /// </summary>
        public bool IsReadOnly { get; }
    }

    /// <summary>
    /// A class representing a message definition.
    /// 
    /// A message is an aggregate of optional fields. Each field is prefixed on the wire by its index in the message. A message may grow in a later version of the schema.
    /// </summary>
    public class MessageDefinition : FieldsDefinition
    {
        public MessageDefinition(string name, Span span, string documentation, BaseAttribute? opcodeAttribute, ICollection<IField> fields) : base(name, span, documentation, opcodeAttribute, fields)
        {
        }
    }

    /// <summary>
    /// Represents an enum definition in a schema.
    /// </summary>
    public class EnumDefinition : Definition
    {
        public EnumDefinition(string name, Span span, string documentation, ICollection<IField> members) : base(name, span, documentation)
        {
            Members = members;
        }
        public ICollection<IField> Members { get; }
    }

    public readonly struct UnionBranch
    {
        public readonly byte Discriminator;
        public readonly TopLevelDefinition Definition;
        public readonly string Documentation;

        public UnionBranch(byte discriminator, TopLevelDefinition definition, string documentation)
        {
            Discriminator = discriminator;
            Definition = definition;
            Documentation = documentation;
        }
    }

    public class UnionDefinition : TopLevelDefinition
    {
        public UnionDefinition(string name, Span span, string documentation, BaseAttribute? opcodeAttribute, ICollection<UnionBranch> branches) : base(name, span, documentation, opcodeAttribute)
        {
            Branches = branches;
        }

        public ICollection<UnionBranch> Branches { get; }
    }
}
