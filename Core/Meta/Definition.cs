using System.Collections.Generic;
using Core.Lexer.Tokenization.Models;
using Core.Meta.Attributes;
using Core.Meta.Interfaces;

namespace Core.Meta
{
    public readonly struct Definition : IDefinition
    {
        public Definition(string name,
            bool isReadOnly,
            in Span span,
            AggregateKind kind,
            ICollection<IField> fields,
            string documentation,
            BaseAttribute? opcodeAttribute)
        {
            Name = name;
            IsReadOnly = isReadOnly;
            Span = span;
            Kind = kind;
            Fields = fields;
            Documentation = documentation;
            OpcodeAttribute = opcodeAttribute;
        }

        public string Name { get; }
        public Span Span { get; }
        public AggregateKind Kind { get; }
        public bool IsReadOnly { get; }
        public ICollection<IField> Fields { get; }
        public string Documentation { get; }
        public BaseAttribute? OpcodeAttribute { get; }
    }
}
