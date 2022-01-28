using System;
using System.Collections.Generic;
using System.IO;
using Core.Lexer.Tokenization;
using Core.Lexer.Tokenization.Models;
using Core.Meta;

namespace Core.Exceptions
{
    public enum Severity
    {
        Warning,
        Error,
    }

    [Serializable]
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
    [Serializable]
    public class SpanException : Exception
    {
       
        public Span Span { get; }
        /// <summary>
        /// A unique error code identifying the type of exception
        /// </summary>
        public int ErrorCode { get; }
        public Severity Severity { get; }

        public SpanException(string message, Span span, int errorCode, Severity severity = Severity.Error) : base(message)
        {
            Span = span;
            ErrorCode = errorCode;
            Severity = severity;
        }
    }
    [Serializable]
    class UnrecognizedTokenException : SpanException
    {
        public UnrecognizedTokenException(char tokenStart, Span span)
            : base($"Unrecognized token start '{tokenStart}'", span, 100) { }
    }
   
    [Serializable]
    class MultipleDefinitionsException : SpanException
    {
        public MultipleDefinitionsException(Definition definition)
            : base($"Multiple definitions for '{definition.Name}'", definition.Span, 101) { }
    }
    [Serializable]
    class ReservedIdentifierException : SpanException
    {
        public ReservedIdentifierException(string identifier, Span span)
            : base($"Use of reserved identifier '{identifier}'", span, 102) { }
    }
    [Serializable]
    class InvalidFieldException : SpanException
    {
        public InvalidFieldException(Field field, string reason)
            : base(reason, field.Span, 103) { }
    }
    [Serializable]
    class UnexpectedTokenException : SpanException
    {
        public UnexpectedTokenException(TokenKind expectedKind, Token token, string? hint = null)
            : base($"Expected {expectedKind}, but found '{token.Lexeme}' of kind {token.Kind}."
                + (string.IsNullOrWhiteSpace(hint) ? "" : $" (Hint: {hint})"), token.Span, 104)
        { }
        public UnexpectedTokenException(Token token, string? hint = null)
            : base($"Encountered unexpected token '{token.Lexeme}' of kind {token.Kind}."
                + (string.IsNullOrWhiteSpace(hint) ? "" : $" (Hint: {hint})"), token.Span, 104)
        { }
        public UnexpectedTokenException(IEnumerable<TokenKind> expectedKinds, Token token, string? hint = null)
            : base($"Expected {string.Join(" or ", expectedKinds)}, but found '{token.Lexeme}' of kind {token.Kind}."
                + (string.IsNullOrWhiteSpace(hint) ? "" : $" (Hint: {hint})"), token.Span, 104)
        { }
    }
    [Serializable]
    class UnrecognizedTypeException : SpanException
    {
        public UnrecognizedTypeException(Token typeName, string containingDefinitionName)
            : base($"Use of unrecognized type name '{typeName.Lexeme}' in definition of '{containingDefinitionName}'", typeName.Span, 105) { }
    }
    [Serializable]
    class InvalidReadOnlyException : SpanException
    {
        public InvalidReadOnlyException(Definition definition)
            : base($"The 'readonly' modifer cannot be applied to '{definition.Name}' as it is not a struct", definition.Span, 106) { }
    }
    [Serializable]
    class InvalidDeprecatedAttributeUsageException : SpanException
    {
        public InvalidDeprecatedAttributeUsageException(Field field)
            : base($"The field '{field.Name}' cannot be marked as 'deprecated' as it is not a member of a message or enum", field.Span, 107) { }
    }
    [Serializable]
    class InvalidOpcodeAttributeUsageException : SpanException
    {
        public InvalidOpcodeAttributeUsageException(Definition definition)
            : base($"The definition '{definition.Name}' cannot be marked with an opcode attribute as it is not a message or struct", definition.Span, 108)
        { }
    }
    [Serializable]
    class InvalidOpcodeAttributeValueException : SpanException
    {
        public InvalidOpcodeAttributeValueException(Definition definition, string reason)
            : base($"The definition '{definition.Name}' was marked with an" +
                $" opcode " +
                $"attribute containing an invalid value: {reason}", definition.Span, 109)
        { }
    }
    [Serializable]
    class DuplicateOpcodeException : SpanException
    {
        public DuplicateOpcodeException(RecordDefinition definition)
            : base($"Multiple definitions for opcode '{definition.OpcodeAttribute?.Value}'", definition.Span, 110) { }
    }

    [Serializable]
    class InvalidMapKeyTypeException : SpanException
    {
        public InvalidMapKeyTypeException(TypeBase type)
            : base($"Type '{type.AsString}' is an invalid key type for a map. Only booleans, numbers, strings, and GUIDs can be used as keys.", type.Span, 111) { }
    }

    [Serializable]
    class DuplicateFieldException : SpanException
    {
        public DuplicateFieldException(Field field, Definition definition)
            : base($"The type '{definition.Name}' already contains a definition for '{field.Name}'.", field.Span, 112) { }
    }

    [Serializable]
    public class InvalidUnionBranchException : SpanException
    {
        public InvalidUnionBranchException(Definition definition)
            : base($"The definition '{definition.Name}' cannot be used as a union branch. Valid union branches are messages and structs.", definition.Span, 113)
        { }
    }

    [Serializable]
    class DuplicateUnionDiscriminatorException : SpanException
    {
        public DuplicateUnionDiscriminatorException(Token discriminator, string unionName)
            : base($"The discriminator index {discriminator.Lexeme} was used more than once in union '{unionName}'.", discriminator.Span, 114)
        { }
    }

    [Serializable]
    class EmptyUnionException : SpanException
    {
        public EmptyUnionException(Definition definition)
            : base($"The definition '{definition.Name}' must contain at least one branch. Valid union branches are messages, structs, or unions.", definition.Span, 115)
        { }
    }

    [Serializable]
    class CyclicDefinitionsException : SpanException
    {
        public CyclicDefinitionsException(Definition definition)
            : base($"The schema contains an invalid cycle of definitions, involving '{definition.Name}'.", definition.Span, 116)
        { }
    }

    [Serializable]
    class ImportFileNotFoundException : SpanException
    {
        public ImportFileNotFoundException(Token pathLiteral)
            : base($"No such file: {pathLiteral.Lexeme}.", pathLiteral.Span, 117)
        { }
    }

    [Serializable]
    class ImportFileReadException : SpanException
    {
        public ImportFileReadException(Token pathToken)
            : base($"Error reading file: {pathToken.Lexeme}.", pathToken.Span, 118)
        { }
    }

    [Serializable]
    class UnsupportedConstTypeException : SpanException
    {
        public UnsupportedConstTypeException(string reason, Span span)
            : base(reason, span, 119)
        { }
    }

    [Serializable]
    class InvalidLiteralException : SpanException
    {
        public InvalidLiteralException(Token token, TypeBase type)
            : base($"'{token.Lexeme}' is an invalid literal for type {type.AsString}.", token.Span, 120)
        { }
    }

    [Serializable]
    class FieldNameException : SpanException
    {
        public FieldNameException(Token token)
            : base($"Field names cannot be the same as their enclosing type.", token.Span, 121)
        { }
    }

    [Serializable]
    class DuplicateConstDefinitionException : SpanException
    {
        public DuplicateConstDefinitionException(ConstDefinition definition)
            : base($"An illegal redefinition for the constant '{definition.Name}' was found in '{Path.GetFileName(definition.Span.FileName)}'", definition.Span, 122)
        { }
    }

    [Serializable]
    public class ReferenceScopeException : SpanException
    {
        public ReferenceScopeException(Definition definition, Definition reference, string scopeLabel)
            : base($"Cannot reference {reference.Name} within {scopeLabel} from {definition.Name}.", definition.Span, 123)
        {}
    }

    [Serializable]
    public class UnknownIdentifierException : SpanException
    {
        public UnknownIdentifierException(Token identifier)
            : base($"The identifier '{identifier.Lexeme}' is not defined at this point. (Try reordering your enum branches.)", identifier.Span, 124)
        { }
    }

    [Serializable]
    public class UnknownAttributeException : SpanException
    {
        public UnknownAttributeException(Token attribute)
            : base($"The attribute '{attribute.Lexeme}' is recognized.", attribute.Span, 125)
        { }
    }

    [Serializable]
    public class UnmatchedParenthesisException : SpanException
    {
        public UnmatchedParenthesisException(Token parenthesis)
            : base($"This parenthesis is unmatched.", parenthesis.Span, 126)
        { }
    }

    [Serializable]
    public class MalformedExpressionException : SpanException
    {
        public MalformedExpressionException(Token token)
            : base($"The expression could not be parsed past this point.", token.Span, 127)
        { }
    }

    [Serializable]
    public class InvalidEnumTypeException : SpanException
    {
        public InvalidEnumTypeException(TypeBase t)
            : base($"Enums must have an integer underlying type, not {t}.", t.Span, 128)
        { }
    }

    [Serializable]
    class DuplicateServiceDiscriminatorException : SpanException
    {
        public DuplicateServiceDiscriminatorException(Token discriminator, string serviceName)
            : base($"The discriminator index {discriminator.Lexeme} was used more than once in service '{serviceName}'.", discriminator.Span, 129)
        { }
    }
    
    [Serializable]
    class DuplicateServiceFunctionNameException : SpanException
    {
        public DuplicateServiceFunctionNameException(ushort discriminator, string serviceName, string functionName, Span span)
            : base($"Index {discriminator} duplicates the function name '{functionName}' which can only be used once in service '{serviceName}'.", span, 130)
        { }
    }

    [Serializable]
    class DuplicateArgumentName : SpanException
    {
        public DuplicateArgumentName(Span span, string serviceName, string serviceIndex, string argumentName)
            : base($"Index {serviceIndex} in service '{serviceName}' has duplicated argument name {argumentName}.", span, 131)
        { }
    }
    
    [Serializable]
    public class EnumZeroWarning : SpanException
    {
        public EnumZeroWarning(Field field)
            : base($"Bebop recommends that 0 in an enum be reserved for a value named 'Unknown', 'Default', or similar. See https://github.com/RainwayApp/bebop/wiki/Why-should-0-be-a-%22boring%22-value-in-an-enum%3F for more info.", field.Span, 200, Severity.Warning)
        { }
    }
}
