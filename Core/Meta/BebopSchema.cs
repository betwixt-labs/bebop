using System;
using System.Collections.Generic;
using System.Linq;
using Core.Exceptions;
using Core.Meta.Interfaces;

namespace Core.Meta
{
    /// <inheritdoc/>
    public readonly struct BebopSchema : ISchema
    {
        public BebopSchema(string nameSpace, Dictionary<string, IDefinition> definitions)
        {
            Namespace = nameSpace;
            Definitions = definitions;
        }
        /// <inheritdoc/>
        public string Namespace { get; }
        /// <inheritdoc/>
        public Dictionary<string, IDefinition> Definitions { get; }


        /// <inheritdoc/>
        public void Validate()
        {

            foreach (var definition in Definitions.Values)
            {
                if (Definitions.Values.Count(d => d.Name.Equals(definition.Name)) > 1)
                {
                    throw new MultipleDefinitionsException(definition);
                }
                if (ReservedWords.Identifiers.Contains(definition.Name))
                {
                    throw new ReservedIdentifierException(definition.Name, definition.Span);
                }
                if (definition.IsReadOnly && !definition.IsStruct())
                {
                    throw new InvalidReadOnlyException(definition);
                }
                if (definition.OpcodeAttribute != null)
                {
                    if (definition.IsEnum())
                    {
                        throw new InvalidOpcodeAttributeUsageException(definition);
                    }
                    if (!definition.OpcodeAttribute.TryValidate(out var opcodeReason))
                    {
                        throw new InvalidOpcodeAttributeValueException(definition, opcodeReason);
                    }
                    if (Definitions.Values.Count(d => d.OpcodeAttribute != null && d.OpcodeAttribute.Value.Equals(definition.OpcodeAttribute.Value)) > 1)
                    {
                        throw new DuplicateOpcodeException(definition);
                    }
                }
                foreach (var field in definition.Fields)
                {
                    if (ReservedWords.Identifiers.Contains(field.Name))
                    {
                        throw new ReservedIdentifierException(field.Name, field.Span);
                    }
                    if (field.DeprecatedAttribute != null && definition.IsStruct())
                    {
                        throw new InvalidDeprecatedAttributeUsageException(field);
                    }
                    switch (definition.Kind)
                    {
                        case AggregateKind.Enum when field.ConstantValue < 0:
                        {
                            throw new InvalidFieldException(field, "Enum values must start at 0");
                        }
                        case AggregateKind.Enum when definition.Fields.Count(f => f.ConstantValue == field.ConstantValue) > 1:
                        {
                            throw new InvalidFieldException(field, "Enum value must be unique");
                        }
                        case AggregateKind.Struct when field.Type is DefinedType dt && definition.Name.Equals(dt.Name):
                        {
                            throw new InvalidFieldException(field, "Struct contains itself");
                        }
                        case AggregateKind.Message when definition.Fields.Count(f => f.ConstantValue == field.ConstantValue) > 1:
                        {
                            throw new InvalidFieldException(field, "Message ID must be unique");
                        }
                        case AggregateKind.Message when field.ConstantValue <= 0:
                        {
                            throw new InvalidFieldException(field, "Message member IDs must start at 1");
                        }
                        case AggregateKind.Message when field.ConstantValue > definition.Fields.Count:
                        {
                            throw new InvalidFieldException(field, "Message ID is greater than field count");
                        }
                        default:
                            break;
                    }
                }
            }
        }
    }
}
