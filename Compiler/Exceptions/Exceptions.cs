using Compiler.Lexer.Tokenization;
using Compiler.Lexer.Tokenization.Models;
using Compiler.Meta.Interfaces;
using System;

namespace Compiler.Exceptions
{
    class SpanException : Exception
    {
        public Span Span { get; }

        public SpanException(string message, Span span) : base(message)
        {
            Span = span;
        }
    }

    class UnrecognizedTokenException : SpanException
    {
        public UnrecognizedTokenException(char tokenStart, Span span)
            : base($"Unrecognized token start '{tokenStart}'", span) { }
    }

    class MultipleDefinitionsException : SpanException
    {
        public MultipleDefinitionsException(IDefinition definition)
            : base($"Multiple definitions for '{definition.Name}'", definition.Span) { }
    }

    class ReservedIdentifierException : SpanException
    {
        public ReservedIdentifierException(string identifier, Span span)
            : base($"Use of reserved identifier '{identifier}'", span) { }
    }

    class InvalidFieldException : SpanException
    {
        public InvalidFieldException(IField field, string reason)
            : base(reason, field.Span) { }
    }

    class UnexpectedTokenException : SpanException
    {
        public UnexpectedTokenException(TokenKind expectedKind, Token token)
            : base($"Expected {expectedKind}, but got '{token.Lexeme}' of kind {token.Kind}", token.Span) { }
    }

    class UnrecognizedTypeException : SpanException
    {
        public UnrecognizedTypeException(Token typeName, string containingDefinitionName)
            : base($"Use of unrecognized type name '{typeName.Lexeme}' in definition of '{containingDefinitionName}'", typeName.Span) { }
    }

    class InvalidReadOnlyException : SpanException
    {
        public InvalidReadOnlyException(IDefinition definition)
            : base($"'{definition.Name}' was declared readonly, but it is not a struct", definition.Span) { }
    }

    class InvalidDeprectedAttributeException : SpanException
    {
        public InvalidDeprectedAttributeException(IField field)
            : base($"'{field.Name}' was marked deprecated, but it is not part of a message", field.Span) { }
    }

    class InvalidMapKeyTypeException : SpanException
    {
        public InvalidMapKeyTypeException(TypeBase type)
            : base($"Type '{type.AsString}' is an invalid key type for a map. Only booleans, numbers, strings, and GUIDs can be used as keys.", type.Span) { }
    }
}
