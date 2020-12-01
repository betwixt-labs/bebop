using System;
using Bebop.Runtime;

namespace Bebop.Exceptions
{
    /// <summary>
    ///     Represents an error that occurs while dynamically accessing Bebop types.
    /// </summary>
    [Serializable]
    public class BebopRuntimeException : Exception
    {
        public BebopRuntimeException(BebopRecord first, BebopRecord second) : base(
            $"Bebop type \"{first.Type.FullName}\" and \"{second.Type.FullName}\" cannot have same opcode\"{first.OpCode}\"")
        {
        }

        public BebopRuntimeException(uint opcode) : base($"A Bebop type with opcode \"{opcode:X}\" does not exist.")
        {
        }

        public BebopRuntimeException(string message)
            : base(message)
        {
        }

        // ReSharper disable once UnusedMember.Global
        public BebopRuntimeException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public BebopRuntimeException(Type first, Type second) : base(
            $"Bebop type \"{first.FullName}\" does not align with \"{second.FullName}\"")
        {
        }

        public BebopRuntimeException()
        {
        }
    }
}