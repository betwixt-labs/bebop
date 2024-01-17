using System;
using System.Text.RegularExpressions;

namespace Core.Meta.Decorators;


public abstract record DecoratorDefinition(string Identifier, string Description, DecoratorTargets Targets, bool AllowMultiple, DecoratorParameter[]? Parameters = null)
{
    public abstract bool TryValidate(out string reason, SchemaDecorator? schemaDecorator = null);
}

public sealed record DecoratorParameter(string Identifier, string Description, BaseType Type, bool IsRequired, string? DefaultValue = null, DecoratorParameterValueValidator? Validator = null)
{
    public bool IsValueAssignable(string value, out string reason)
    {
        switch (Type)
        {
            case BaseType.String:
                if (string.IsNullOrEmpty(value))
                {
                    reason = "String values cannot be empty.";
                    return false;
                }
                reason = string.Empty;
                return true;
            case BaseType.Bool:
                if (string.IsNullOrWhiteSpace(value))
                {
                    reason = "Bool values cannot be empty.";
                    return false;
                }
                if (bool.TryParse(value, out _))
                {
                    reason = string.Empty;
                    return true;
                }
                reason = $"\"{value}\" is not a valid bool.";
                return false;
            case BaseType.Byte:
                if (string.IsNullOrWhiteSpace(value))
                {
                    reason = "Byte values cannot be empty.";
                    return false;
                }
                if (byte.TryParse(value, out _))
                {
                    reason = string.Empty;
                    return true;
                }
                reason = $"\"{value}\" is not a valid byte.";
                return false;
            case BaseType.UInt16:
                if (string.IsNullOrWhiteSpace(value))
                {
                    reason = "UInt16 values cannot be empty.";
                    return false;
                }
                if (ushort.TryParse(value, out _))
                {
                    reason = string.Empty;
                    return true;
                }
                reason = $"\"{value}\" is not a valid UInt16.";
                return false;
            case BaseType.Int16:
                if (string.IsNullOrWhiteSpace(value))
                {
                    reason = "Int16 values cannot be empty.";
                    return false;
                }
                if (short.TryParse(value, out _))
                {
                    reason = string.Empty;
                    return true;
                }
                reason = $"\"{value}\" is not a valid Int16.";
                return false;
            case BaseType.UInt32:
                if (string.IsNullOrWhiteSpace(value))
                {
                    reason = "UInt32 values cannot be empty.";
                    return false;
                }
                if (uint.TryParse(value, out _))
                {
                    reason = string.Empty;
                    return true;
                }
                reason = $"\"{value}\" is not a valid UInt32.";
                return false;
            case BaseType.Int32:
                if (string.IsNullOrWhiteSpace(value))
                {
                    reason = "Int32 values cannot be empty.";
                    return false;
                }
                if (int.TryParse(value, out _))
                {
                    reason = string.Empty;
                    return true;
                }
                reason = $"\"{value}\" is not a valid Int32.";
                return false;
            case BaseType.UInt64:
                if (string.IsNullOrWhiteSpace(value))
                {
                    reason = "UInt64 values cannot be empty.";
                    return false;
                }
                if (ulong.TryParse(value, out _))
                {
                    reason = string.Empty;
                    return true;
                }
                reason = $"\"{value}\" is not a valid UInt64.";
                return false;
            case BaseType.Int64:
                if (string.IsNullOrWhiteSpace(value))
                {
                    reason = "Int64 values cannot be empty.";
                    return false;
                }
                if (long.TryParse(value, out _))
                {
                    reason = string.Empty;
                    return true;
                }
                reason = $"\"{value}\" is not a valid Int64.";
                return false;
            case BaseType.Float32:
                if (string.IsNullOrWhiteSpace(value))
                {
                    reason = "Float32 values cannot be empty.";
                    return false;
                }
                if (float.TryParse(value, out _))
                {
                    reason = string.Empty;
                    return true;
                }
                reason = $"\"{value}\" is not a valid Float32.";
                return false;
            case BaseType.Float64:
                if (string.IsNullOrWhiteSpace(value))
                {
                    reason = "Float64 values cannot be empty.";
                    return false;
                }
                if (double.TryParse(value, out _))
                {
                    reason = string.Empty;
                    return true;
                }
                reason = $"\"{value}\" is not a valid Float64.";
                return false;
            case BaseType.Guid:
                if (string.IsNullOrWhiteSpace(value))
                {
                    reason = "Guid values cannot be empty.";
                    return false;
                }
                if (Guid.TryParse(value, out _))
                {
                    reason = string.Empty;
                    return true;
                }
                reason = $"\"{value}\" is not a valid Guid.";
                return false;
        }
        reason = $"\"{value}\" is not a valid {Type}.";
        return false;
    }
}

public sealed record DecoratorParameterValueValidator(Regex Pattern, string Reason)
{

}