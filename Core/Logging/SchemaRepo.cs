using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Errata;

namespace Core.Logging
{

    public sealed class SchemaRepo : ISourceRepository
    {
        public bool TryGet(string id, [NotNullWhen(true)] out Source? source)
        {
            source = new Source(id, File.ReadAllText(id));
            return true;
        }
    }
}