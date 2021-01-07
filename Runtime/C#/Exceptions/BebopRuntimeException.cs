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
        /// <inheritdoc />
        public BebopRuntimeException(BebopRecord first, BebopRecord second) : base(
            $"Bebop type '{first.Type.FullName}' and '{second.Type.FullName}' cannot have same opcode'{first.OpCode}'")
        {
        }
        /// <inheritdoc />
        public BebopRuntimeException(uint opcode) : base($"A Bebop type with opcode 0x'{opcode:X}' does not exist.")
        {
        }
        /// <inheritdoc />
        public BebopRuntimeException(string message)
            : base(message)
        {
        }
        /// <inheritdoc />
        // ReSharper disable once UnusedMember.Global
        public BebopRuntimeException(string message, Exception inner)
            : base(message, inner)
        {
        }
        /// <inheritdoc />
        public BebopRuntimeException(Type first, Type second) : base(
            $"Bebop type '{first.FullName}' does not align with '{second.FullName}'")
        {
        }
        /// <inheritdoc />
        public BebopRuntimeException()
        {
        }
    }
}