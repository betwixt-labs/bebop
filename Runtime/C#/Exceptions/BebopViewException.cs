using System;

namespace Bebop.Exceptions
{
    /// <summary>
    ///     Represents an error that occurs during Bebop reading and writing
    /// </summary>
    [Serializable]
    public class BebopViewException : Exception
    {
        public BebopViewException()
        {
        }

        public BebopViewException(string message)
            : base(message)
        {
        }

        public BebopViewException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}