using System;
using Core.Lexer.Tokenization;
using Core.Lexer.Tokenization.Models;
using Core.Meta.Interfaces;

namespace Core.Exceptions
{
    public class CompilerException : Exception
    {
        /// <summary>
        /// A unique error code identifying the type of exception
        /// </summary>
        public int ErrorCode => 600;
        public CompilerException(string message) : base(message)
        {
        }
    }    
    
    public class SpanException : Exception
    {
       
        public Span Span { get; }
        /// <summary>
        /// A unique error code identifying the type of exception
        /// </summary>
        public int ErrorCode { get; }

        public SpanException(string message, Span span, int errorCode) : base(message)
        {
            Span = span;
            ErrorCode = errorCode;
        }
    }

    class UnrecognizedTokenException : SpanException
    {
        public UnrecognizedTokenException(char tokenStart, Span span)
            : base($"Unrecognized token start '{tokenStart}'", span, 100) { }
    }

    class MultipleDefinitionsException : SpanException
    {
        public MultipleDefinitionsException(IDefinition definition)
            : base($"Multiple definitions for '{definition.Name}'", definition.Span, 101) { }
    }

    class ReservedIdentifierException : SpanException
    {
        public ReservedIdentifierException(string identifier, Span span)
            : base($"Use of reserved identifier '{identifier}'", span, 102) { }
    }

    class InvalidFieldException : SpanException
    {
        public InvalidFieldException(IField field, string reason)
            : base(reason, field.Span, 103) { }
    }

    class UnexpectedTokenException : SpanException
    {
        public UnexpectedTokenException(TokenKind expectedKind, Token token)
            : base($"Expected {expectedKind}, but got '{token.Lexeme}' of kind {token.Kind}", token.Span, 104) { }
    }

    class UnrecognizedTypeException : SpanException
    {
        public UnrecognizedTypeException(Token typeName, string containingDefinitionName)
            : base($"Use of unrecognized type name '{typeName.Lexeme}' in definition of '{containingDefinitionName}'", typeName.Span, 105) { }
    }

    class InvalidReadOnlyException : SpanException
    {
        public InvalidReadOnlyException(IDefinition definition)
            : base($"'{definition.Name}' was declared readonly, but it is not a struct", definition.Span, 106) { }
    }

    class InvalidDeprecatedAttributeUsageException : SpanException
    {
        public InvalidDeprecatedAttributeUsageException(IField field)
            : base($"'{field.Name}' was marked " +
                $"deprecated" +
                $", but it is not part of a message", field.Span, 107) { }
    }

    class InvalidOpcodeAttributeUsageException : SpanException
    {
        public InvalidOpcodeAttributeUsageException(IDefinition definition)
            : base($"'{definition.Name}' was marked " +
                $"opcode" +
                $", but it is not part of a message or struct", definition.Span, 108)
        { }
    }
    class InvalidOpcodeAttributeValueException : SpanException
    {
        public InvalidOpcodeAttributeValueException(IDefinition definition, string reason)
            : base($"'{definition.Name}' was marked " +
                $"opcode" +
                $", however it's value is invalid: {reason}", definition.Span, 109)
        { }
    }

    class DuplicateOpcodeException : SpanException
    {
        public DuplicateOpcodeException(IDefinition definition)
            : base($"Multiple definitions for opcode '{definition.OpcodeAttribute?.Value}'", definition.Span, 110) { }
    }


    class InvalidMapKeyTypeException : SpanException
    {
        public InvalidMapKeyTypeException(TypeBase type)
            : base($"Type '{type.AsString}' is an invalid key type for a map. Only booleans, numbers, strings, and GUIDs can be used as keys.", type.Span, 111) { }
    }
}
