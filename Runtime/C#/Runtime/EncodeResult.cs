using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bebop.Runtime
{
    /// <summary>
    /// Indicates the result of writing to a buffer
    /// </summary>
    public enum EncodeResult : byte
    {
        /// <summary>
        /// Default value, unused
        /// </summary>
        Unknown,

        /// <summary>
        /// Writing to the buffer succeeded with no errors
        /// </summary>
        Success,

        /// <summary>
        /// Writing to the buffer did not complete because a given buffer to write into was not big enough to write to
        /// </summary>
        InputBufferTooSmall
    }
}
