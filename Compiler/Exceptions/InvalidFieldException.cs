using System;

namespace Compiler.Exceptions
{
    public class InvalidFieldException : Exception
    {
        public InvalidFieldException(string fieldName, string reason, uint line, uint column, string fileName) : base($"The field {fieldName} " +
            $"{reason} in {fileName} starting at line {line + 1}:{column + 1}")
        {

        }
    }
}
