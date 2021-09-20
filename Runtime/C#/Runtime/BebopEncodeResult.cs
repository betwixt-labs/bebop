using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bebop.Runtime
{
    /// <summary>
    /// Details the result of attempting to encode a record to a buffer
    /// </summary>
    public readonly struct BebopEncodeResult
    {
        /// <summary></summary>
        public BebopEncodeResult(EncodeResult result, int written)
        {
            Result = result;
            Written = written;
        }

        /// <summary>
        /// The result of encoding. If this succeeded, or a specific failure type
        /// </summary>
        public readonly EncodeResult Result;

        /// <summary>
        /// The amount of bytes written to the buffer
        /// </summary>
        public readonly int Written;
    }
}
