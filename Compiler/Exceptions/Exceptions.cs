using Compiler.Lexer.Tokenization;
using Compiler.Lexer.Tokenization.Models;
using Compiler.Meta.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler.Exceptions
{
    class SpanException : Exception
    {
        public Span Span { get; }
        public string SourcePath { get; }

        public SpanException(string message, Span span, string sourcePath) : base(message)
        {
            Span = span;
            SourcePath = sourcePath;
        }
    }

    class UnrecognizedTokenException : SpanException
    {
        public UnrecognizedTokenException(char tokenStart, Span span, string sourcePath)
            : base($"Unrecognized token start '{tokenStart}'", span, sourcePath) { }
    }

    class MultipleDefinitionsException : SpanException
    {
        public MultipleDefinitionsException(IDefinition definition, string sourcePath)
            : base($"Multiple definitions for '{definition.Name}'", definition.Span, sourcePath) { }
    }

    class ReservedIdentifierException : SpanException
    {
        public ReservedIdentifierException(string identifier, Span span, string sourcePath)
            : base($"Use of reserved identifier '{identifier}'", span, sourcePath) { }
    }

    class InvalidFieldException : SpanException
    {
        public InvalidFieldException(IField field, string reason, string sourcePath)
            : base(reason, field.Span, sourcePath) { }
    }

    class UnexpectedTokenException : SpanException
    {
        public UnexpectedTokenException(TokenKind expectedKind, Token token, string sourcePath)
            : base($"Expected {expectedKind}, but got '{token.Lexeme}' of kind {token.Kind}", token.Span, sourcePath) { }
    }

    class UnrecognizedTypeException : SpanException
    {
        public UnrecognizedTypeException(Token typeName, string containingDefinitionName, string sourcePath)
            : base($"Use of unrecognized type name '{typeName.Lexeme}' in definition of '{containingDefinitionName}'", typeName.Span, sourcePath) { }
    }

    class InvalidReadOnlyException : SpanException
    {
        public InvalidReadOnlyException(IDefinition definition, string sourcePath)
            : base($"'{definition.Name}' was declared readonly, but it is not a struct", definition.Span, sourcePath) { }
    }

    class InvalidDeprectedAttributeException : SpanException
    {
        public InvalidDeprectedAttributeException(IField field, string sourcePath)
            : base($"'{field.Name}' was marked deprecated, but it is not part of a message", field.Span, sourcePath) { }
    }
}
