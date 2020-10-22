using System.Collections.Generic;
using Compiler.Lexer.Tokenization.Models;
using Compiler.Meta.Interfaces;

namespace Compiler.Meta
{
    public readonly struct Definition : IDefinition
    {
        public Definition(string name, bool isReadOnly, in Span span, AggregateKind kind, ICollection<IField> fields, string documentation)
        {
            Name = name;
            IsReadOnly = isReadOnly;
            Span = span;
            Kind = kind;
            Fields = fields;
            Documentation = documentation;
        }

        public string Name { get; }
        public Span Span { get; }
        public AggregateKind Kind { get; }
        public bool IsReadOnly { get; }
        public ICollection<IField> Fields { get; }
        public string Documentation { get; }
    }
}
