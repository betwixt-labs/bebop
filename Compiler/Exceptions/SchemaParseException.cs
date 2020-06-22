using System;

namespace Compiler.Exceptions
{
    public class SchemaParseException : Exception
    {
        public SchemaParseException()
        {
        }

        public SchemaParseException(string message) : base(message)
        {
        }

        public SchemaParseException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}