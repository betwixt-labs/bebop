using System;

namespace Compiler.Exceptions
{
    public class TypeUndefinedException : Exception
    {
        public TypeUndefinedException()
        {
        }

        public TypeUndefinedException(string undefinedType, string definitionName, int line, int column, string fileName) : base($"Undefined type {undefinedType} " +
            $"declared as member of aggregate {definitionName} in {fileName} at line {line+1}:{column + 1}")
        {

        }

        public TypeUndefinedException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}