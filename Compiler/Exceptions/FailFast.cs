using Compiler.Lexer.Tokenization;
using Compiler.Lexer.Tokenization.Models;
using Compiler.Meta.Interfaces;

namespace Compiler.Exceptions
{
    public static class FailFast
    {
        public static UnexpectedTypeException ExpectedTypeException(TokenKind expected, in Token token , string source)
        {
            return new UnexpectedTypeException(expected.ToString(), $"{token.Lexeme} ({token.Kind})", token.Position.StartLine, token.Position.StartColumn, source);
        }

        public static TypeUndefinedException UndefinedTypeException(in Token token, in Token definitionToken, string source)
        {
            return new TypeUndefinedException(token.ToString(), $"{definitionToken.Lexeme} ({definitionToken.Kind})", token.Position.StartLine, token.Position.StartColumn, source);
        }
        public static DuplicateTypeException DuplicateException(IDefinition definition, string source)
        {
            return new DuplicateTypeException(definition.Name, definition.Line, definition.Column, source);
        }

        public static InvalidFieldException InvalidField(IField field, string reason, string source)
        {
            return new InvalidFieldException(field.Name, reason, field.Line, field.Column, source);
        }

        public static ReservedTypeException ReservedException(IDefinition definition, string source)
        {
            return new ReservedTypeException(definition.Name, definition.Line, definition.Column, source);
        }

        public static UnsupportedOperationException RecursiveException(IDefinition definition, string source)
        {
            return new UnsupportedOperationException($"Nesting of the struct \"{definition.Name}\" is not allowed", (int) definition.Line, (int) definition.Column, source);
        }

        public static UnsupportedOperationException UnsupportedException(TokenKind declaration, TokenKind required, in Token token, string source)
        {
            return new UnsupportedOperationException($"The \"{declaration}\" declaration may only be applied to \"{required}\" fields", token.Position.StartLine, token.Position.StartColumn, source);
        }
    }
}
