using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bebop.Runtime
{
    /// <summary>
    /// Indicates the kind of a Bebop type.
    /// </summary>
    public enum BebopKind
    {
        /// <summary>
        /// The Bebop type if an enum 
        /// </summary>
        Enum,
        /// <summary>
        /// The Bebop type is defined as a struct.
        /// </summary>
        Struct,
        /// <summary>
        /// The Bebop type is defined as a message.
        /// </summary>
        Message,
        /// <summary>
        /// The Bebop type is defined a union.
        /// </summary>
        Union
    }
}
