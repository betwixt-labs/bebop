using System;

namespace Compiler.Exceptions
{
    public class DuplicateTypeException : Exception
    {
        public DuplicateTypeException(string definitionName, uint line, uint column, string fileName) : base($"The type {definitionName} is" +
            $"declared more than once in {fileName} starting at line {line + 1}:{column + 1}")
        {

        }
    }
}
