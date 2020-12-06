using System;

namespace Bebop.Exceptions
{
    /// <summary>
    ///     Represents an error that occurs during Bebop reading and writing
    /// </summary>
    [Serializable]
    public class BebopViewException : Exception
    {
        /// <inheritdoc />
        public BebopViewException()
        {
        }

        /// <inheritdoc />
        public BebopViewException(string message)
            : base(message)
        {
        }

        /// <inheritdoc />
        public BebopViewException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}