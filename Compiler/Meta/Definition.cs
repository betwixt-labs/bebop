using System.Collections.Generic;
using Compiler.Meta.Interfaces;

namespace Compiler.Meta
{
    public readonly struct Definition : IDefinition
    {
        public string Name { get; }
        public uint Line { get; }
        public uint Column { get; }
        public AggregateKind Kind { get; }
        public ICollection<IField> Fields { get; }
    }
}
