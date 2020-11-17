using System;

namespace Bebop.Exceptions
{
    /// <summary>
    ///     Represents an error that occurs while dynamically accessing Bebop types.
    /// </summary>
    public class BebopRuntimeException : Exception
    {
        public BebopRuntimeException(BebopRecord first, BebopRecord second) : base($"Bebop type \"{first.Class.FullName}\" and \"{second.Class.FullName}\" cannot have same opcode\"{first.OpCode}\"")
        {
        }

        public BebopRuntimeException(uint opcode) : base($"A Bebop type with opcode \"{opcode:X}\" does not exist."){}

        public BebopRuntimeException(string message)
            : base(message)
        {
        }

        public BebopRuntimeException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public BebopRuntimeException(Type first, Type second) : base($"Bebop type \"{first.FullName}\" does not align with \"{second.FullName}\"")
        {
        }
    }
}