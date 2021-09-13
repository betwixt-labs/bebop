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
    public enum WriteResult
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
        /// Writing to the buffer succeeded, but the input buffer was replaced with a new buffer instance larger than the original
        /// </summary>
        SuccessButGrewBuffer
    }
}
