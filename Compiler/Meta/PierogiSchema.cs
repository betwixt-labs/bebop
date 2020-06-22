using System.Collections.Generic;
using Compiler.Meta.Interfaces;

namespace Compiler.Meta
{
    public readonly struct PierogiSchema : ISchema
    {
        public string Package { get; }
        public ICollection<IDefinition> Definitions { get; }
    }
}
