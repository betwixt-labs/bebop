using System;

namespace Compiler.Exceptions
{
    public class UnexpectedTypeException : Exception
    {
        public UnexpectedTypeException()
        {
        }

        public UnexpectedTypeException(string expectedToken, string foundToken, int line, int column, string fileName) : base($"Expected type: {expectedToken}. " +
            $"Actual type: {foundToken} in {fileName} at line {line + 1}:{column + 1}")
        {

        }

        public UnexpectedTypeException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}