using System.Collections.Generic;
using Compiler.Meta.Interfaces;

namespace Compiler.Meta
{
    public readonly struct Definition : IDefinition
    {
        public Definition(string name,
            in uint line,
            in uint column,
            AggregateKind kind,
            ICollection<IField> fields)
        {
            Name = name;
            Line = line;
            Column = column;
            Kind = kind;
            Fields = fields;
        }

        public string Name { get; }
        public uint Line { get; }
        public uint Column { get; }
        public AggregateKind Kind { get; }
        public ICollection<IField> Fields { get; }
    }
}
