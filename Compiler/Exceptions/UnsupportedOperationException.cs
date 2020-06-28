using System;

namespace Compiler.Exceptions
{
    public class UnsupportedOperationException : Exception
    {
        public UnsupportedOperationException()
        {
        }

        public UnsupportedOperationException(string message, int line, int column, string fileName) : base(message+
            $" in {fileName} at line {line + 1}:{column + 1}")
        {

        }

        public UnsupportedOperationException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}