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
    public readonly struct BebopWriteResult
    {
        /// <summary></summary>
        public BebopWriteResult(WriteResult result, int written, byte[] buffer)
        {
            Result = result;
            Written = written;
            Buffer = buffer;
        }

        /// <summary>
        /// The result of encoding. If this succeeded, or a specific failure type
        /// </summary>
        public WriteResult Result { get; }

        /// <summary>
        /// The amount of bytes written to the buffer
        /// </summary>
        public int Written { get; }

        /// <summary>
        /// The final instance of the buffer written to. If Bebop Writer was given a buffer to start with, but had to grow it to fit the fill serialized message, this will be a different instance than what was passed in.
        /// </summary>
        public byte[] Buffer { get; }
    }
}
