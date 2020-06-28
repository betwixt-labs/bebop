using System;

namespace Compiler.Exceptions
{
    public class ReservedTypeException : Exception
    {
        public ReservedTypeException(string definitionName, uint line, uint column, string fileName) : base($"The type {definitionName} is" +
            $"using a reserved keyword in {fileName} starting at line {line + 1}:{column + 1}")
        {

        }
    }
}
