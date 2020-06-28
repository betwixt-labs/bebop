using System.Collections.Generic;
using Compiler.Meta.Interfaces;

namespace Compiler.Meta
{
    public readonly struct PierogiSchema : ISchema
    {
        public PierogiSchema(string package, ICollection<IDefinition> definitions)
        {
            Package = package;
            Definitions = definitions;
        }

        public string Package { get; }
        public ICollection<IDefinition> Definitions { get; }
    }
}
