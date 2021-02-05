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
        public BebopSchema(string nameSpace, Dictionary<string, Definition> definitions)
        {
            Namespace = nameSpace;
            Definitions = definitions;
        }
        /// <inheritdoc/>
        public string Namespace { get; }
        /// <inheritdoc/>
        public Dictionary<string, Definition> Definitions { get; }


        /// <inheritdoc/>
        public void Validate()
        {

            foreach (var definition in Definitions.Values)
            {
                if (Definitions.Values.Count(d => d.Name.Equals(definition.Name, StringComparison.OrdinalIgnoreCase)) > 1)
                {
                    throw new MultipleDefinitionsException(definition);
                }
                if (ReservedWords.Identifiers.Contains(definition.Name))
                {
                    throw new ReservedIdentifierException(definition.Name, definition.Span);
                }
                if (definition is TopLevelDefinition td && td.OpcodeAttribute != null)
                {
                    if (!td.OpcodeAttribute.TryValidate(out var opcodeReason))
                    {
                        throw new InvalidOpcodeAttributeValueException(td, opcodeReason);
                    }
                    if (Definitions.Values.Count(d => d is TopLevelDefinition td2 && td2.OpcodeAttribute != null && td2.OpcodeAttribute.Value.Equals(td.OpcodeAttribute.Value)) > 1)
                    {
                        throw new DuplicateOpcodeException(td);
                    }
                }
                if (definition is FieldsDefinition fd) foreach (var field in fd.Fields)
                {
                    if (ReservedWords.Identifiers.Contains(field.Name))
                    {
                        throw new ReservedIdentifierException(field.Name, field.Span);
                    }
                    if (field.DeprecatedAttribute != null && fd is StructDefinition)
                    {
                        throw new InvalidDeprecatedAttributeUsageException(field);
                    }
                    if (fd.Fields.Count(f => f.Name.Equals(field.Name, StringComparison.OrdinalIgnoreCase)) > 1)
                    {
                        throw new DuplicateFieldException(field, fd);
                    }
                    switch (fd)
                    {
                        case StructDefinition when field.Type is DefinedType dt && fd.Name.Equals(dt.Name):
                        {
                            throw new InvalidFieldException(field, "Struct contains itself");
                        }
                        case MessageDefinition when fd.Fields.Count(f => f.ConstantValue == field.ConstantValue) > 1:
                        {
                            throw new InvalidFieldException(field, "Message index must be unique");
                        }
                        case MessageDefinition when field.ConstantValue <= 0:
                        {
                            throw new InvalidFieldException(field, "Message member index must start at 1");
                        }
                        case MessageDefinition when field.ConstantValue > fd.Fields.Count:
                        {
                            throw new InvalidFieldException(field, "Message index is greater than field count");
                        }
                        default:
                            break;
                    }
                }
                if (definition is EnumDefinition ed)
                {
                    HashSet<uint> seen = new HashSet<uint>();
                    foreach (var field in ed.Members)
                    {
                        if (seen.Contains(field.ConstantValue))
                        {
                            throw new InvalidFieldException(field, "Enum value must be unique");
                        }
                        seen.Add(field.ConstantValue);
                    }
                }
            }
        }
    }
}
