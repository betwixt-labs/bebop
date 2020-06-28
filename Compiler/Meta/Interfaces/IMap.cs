using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler.Meta.Interfaces
{
    interface IMap
    {
        int KeyTypeCode { get; }

        int ValueTypeCode { get; }
    }
}
