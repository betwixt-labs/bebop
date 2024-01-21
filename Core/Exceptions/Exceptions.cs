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
        public CompilerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    [Serializable]
    public class MalformedBebopConfigException : Exception
    {
        /// <summary>
        /// A unique error code identifying the type of exception
        /// </summary>
        public int ErrorCode => 601;
        public MalformedBebopConfigException(string message) : base($"Error parsing bebop.json: {message}")
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
        public string? Hint { get; }

        public SpanException(string message, Span span, int errorCode, string? hint = null, Severity severity = Severity.Error) : base(message)
        {
            Span = span;
            ErrorCode = errorCode;
            Severity = severity;
            Hint = hint;
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
            : base($"Expected type {expectedKind}, but found '{token.Lexeme}' of kind {token.Kind}."
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
    class InvalidDeprecatedDecoratorUsageException : SpanException
    {
        public InvalidDeprecatedDecoratorUsageException(Field field)
            : base($"The field '{field.Name}' cannot be marked as 'deprecated' as it is not a member of a message or enum", field.Span, 107) { }
    }
    [Serializable]
    class InvalidOpcodeDecoratorUsageException : SpanException
    {
        public InvalidOpcodeDecoratorUsageException(Definition definition)
            : base($"The definition '{definition.Name}' cannot be marked with an opcode identifier as it is not a message or struct", definition.Span, 108)
        { }
    }
    [Serializable]
    class InvalidOpcodeDecoratorValueException : SpanException
    {
        public InvalidOpcodeDecoratorValueException(Definition definition, string reason)
            : base($"The definition '{definition.Name}' was marked with an" +
                $" opcode " +
                $"identifier containing an invalid value: {reason}", definition.Span, 109)
        { }
    }
    [Serializable]
    class DuplicateOpcodeException : SpanException
    {
        public DuplicateOpcodeException(RecordDefinition definition)
            : base($"Multiple definitions for opcode '{definition.OpcodeDecorator?.Arguments["fourcc"]}'", definition.Span, 110) { }
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
        { }
    }

    [Serializable]
    public class UnknownIdentifierException : SpanException
    {
        public UnknownIdentifierException(Token identifier)
            : base($"The decorator '{identifier.Lexeme}' is not defined at this point. (Try reordering your enum branches.)", identifier.Span, 124)
        { }
    }

    [Serializable]
    public class UnknownDecoratorException : SpanException
    {
        public UnknownDecoratorException(Token identifier)
            : base($"The decorator '{identifier.Lexeme}' is not defined.", identifier.Span, 125)
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
    class DuplicateServiceMethodNameException : SpanException
    {
        public DuplicateServiceMethodNameException(string serviceName, string methodName, Span span)
            : base($"Method '{methodName}' is defined multiple times in '{serviceName}'.", span, 130)
        { }
    }

    [Serializable]
    class DuplicateServiceMethodIdException : SpanException
    {
        public DuplicateServiceMethodIdException(uint id, string serviceName, string methodName, Span span)
            : base($"Method id '{id}' for '{methodName}' collides with another method in service '{serviceName}'.", span, 131)
        { }
    }

    [Serializable]
    class InvalidServiceRequestTypeException : SpanException
    {
        public InvalidServiceRequestTypeException(string serviceName, string methodName, TypeBase type, Span span)
              : base($"The request type of method '{methodName}' in service '{serviceName}' is '{type.AsString}'  must be a defined type of message, struct, or union.", span, 132)
        { }
    }
    [Serializable]
    class InvalidServiceReturnTypeException : SpanException
    {
        public InvalidServiceReturnTypeException(string serviceName, string methodName, TypeBase type, Span span)
            : base($"The return type of method '{methodName}' in service '{serviceName}' is '{type.AsString}' but must be a defined type of message, struct, or union.", span, 133)
        { }
    }

    [Serializable]
    class ServiceMethodIdCollisionException : SpanException
    {
        public ServiceMethodIdCollisionException(string serviceOneName, string methodOneName, string serviceTwoName, string methodTwoName, uint id, Span span)
            : base($"The hashed ID of service '{serviceOneName}' and method '{methodOneName}' collides with the hashed ID of service '{serviceTwoName}' and '{methodTwoName}' (id: {id}).", span, 134)
        { }
    }

    [Serializable]
    class UnexpectedEndOfFile : SpanException
    {
        public UnexpectedEndOfFile(Span span)
            : base($"Unexpected EOF.", span, 135)
        { }
    }

    [Serializable]
    class MissingArgumentException : SpanException
    {
        public MissingArgumentException(string identifier, string paramName, int expectedArgCount, int foundArgCount, Span span, string? hint = null)
            : base($"Expected {expectedArgCount} arguments for '{identifier}', found {foundArgCount}. Missing '{paramName}'." + (string.IsNullOrWhiteSpace(hint) ? "" : $" (Hint: {hint})"), span, 136)
        { }
    }
    class InvalidArgumentTypeException : SpanException
    {
        public InvalidArgumentTypeException(string identifier, string paramName, BaseType expectedType, TokenKind foundKind, Span span, string? hint = null)
            : base($"Expected argument for '{paramName}' of '{identifier}' to be of type '{expectedType}', found '{foundKind}'." + (string.IsNullOrWhiteSpace(hint) ? "" : $" (Hint: {hint})"), span, 137)
        { }
    }

    class InvalidArgumentValueException : SpanException
    {
        public InvalidArgumentValueException(string identifier, string paramName, string reason, Span span, string? hint = null)
            : base($"Invalid value for argument '{paramName}' of '{identifier}': {reason}." + (string.IsNullOrWhiteSpace(hint) ? "" : $" (Hint: {hint})"), span, 138)
        { }
    }

    class InvalidNamedArgumentException : SpanException
    {
        public InvalidNamedArgumentException(string identifier, string paramName, Span span, string? hint = null)
            : base($"Decorator '{identifier}' does not have a parameter named '{paramName}' " + (string.IsNullOrWhiteSpace(hint) ? "" : $" (Hint: {hint})"), span, 139)
        { }
    }

    class MultipleDecoratorsException : SpanException
    {
        public MultipleDecoratorsException(string identifier, Span span, string? hint = null)
            : base($"The decorator '{identifier}' cannot be defined multiple times on a single target." + (string.IsNullOrWhiteSpace(hint) ? "" : $" (Hint: {hint})"), span, 140)
        { }
    }


    class InvalidDecoratorUsageException : SpanException
    {
        public InvalidDecoratorUsageException(string identifier, string reason, Span span, string? hint = null)
             : base($"Decorator '{identifier}' cannot be applied to this target: {reason}." + (string.IsNullOrWhiteSpace(hint) ? "" : $" (Hint: {hint})"), span, 141)
        { }
    }
    class DecoratorValidationException : SpanException
    {
        public DecoratorValidationException(string identifier, string reason, Span span, string? hint = null)
             : base($"Decorator '{identifier}' is invalid: {reason}." + (string.IsNullOrWhiteSpace(hint) ? "" : $" (Hint: {hint})"), span, 142)
        { }
    }

    class StackSizeExceededException : SpanException
    {
        public StackSizeExceededException(string reason, Span span, string? hint = null)
             : base($"Stack size exceeded: {reason}", span, 143)
        { }
    }

    class UnknownParameterException : SpanException
    {
        public UnknownParameterException(string identifier, string paramName, Span span, string? hint = null)
             : base($"Decorator '{identifier}' does not have a parameter named '{paramName}'.", span, 144, hint)
        { }
    }

    class TooManyArgumentsException : SpanException
    {
        public TooManyArgumentsException(string identifier, int expectedArgCount, int foundArgCount, Span span, string? hint = null)
             : base($"Expected {expectedArgCount} arguments for '{identifier}', found {foundArgCount}.", span, 145, hint)
        { }
    }

    class MissingValueForArgumentException : SpanException
    {
        public MissingValueForArgumentException(string identifier, string paramName, Span span, string? hint = null)
             : base($"Decorator '{identifier}' is missing a value for parameter '{paramName}'.", span, 146, hint)
        { }
    }
    [Serializable]
    public class EnvironmentVariableNotFoundException : SpanException
    {
        public EnvironmentVariableNotFoundException(Span span, string reason)
            : base(reason, span, 147, severity: Severity.Error)
        { }
    }



    [Serializable]
    public class EnumZeroWarning : SpanException
    {
        public EnumZeroWarning(Field field)
            : base($"Bebop recommends that 0 in an enum be reserved for a value named 'Unknown', 'Default', or similar.", field.Span, 200, " See https://github.com/betwixt-labs/bebop/wiki/Why-should-0-be-a-%22boring%22-value-in-an-enum%3F for more info.", Severity.Warning)
        { }
    }

    [Serializable]
    public class DeprecatedFeatureWarning : SpanException
    {
        public DeprecatedFeatureWarning(Span span, string reason)
            : base(reason, span, 201, severity: Severity.Warning)
        { }
    }

}
