using System.Collections.Generic;
using System.Linq;
using Compiler.Exceptions;
using Compiler.Meta.Interfaces;

namespace Compiler.Meta
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
                foreach (var field in definition.Fields)
                {
                    if (ReservedWords.Identifiers.Contains(field.Name))
                    {
                        throw new ReservedIdentifierException(field.Name, field.Span);
                    }
                    if (field.DeprecatedAttribute.HasValue && !definition.IsMessage())
                    {
                        throw new InvalidDeprectedAttributeException(field);
                    }
                    switch (definition.Kind)
                    {
                        case AggregateKind.Enum:
                            if (field.ConstantValue < 0)
                            {
                                throw new InvalidFieldException(field, "Enum values must start at 0");
                            }
                            if (definition.Fields.Count(f => f.ConstantValue == field.ConstantValue) > 1)
                            {
                                throw new InvalidFieldException(field, "Enum value must be unique");
                            }
                            break;
                        case AggregateKind.Struct:
                            if (field.Type is DefinedType dt && definition.Name.Equals(dt.Name))
                            {
                                throw new InvalidFieldException(field, "Struct contains itself");
                            }
                            break;
                        case AggregateKind.Message:
                            if (definition.Fields.Count(f => f.ConstantValue == field.ConstantValue) > 1)
                            {
                                throw new InvalidFieldException(field, "Message ID must be unique");
                            }
                            if (field.ConstantValue <= 0)
                            {
                                throw new InvalidFieldException(field, "Message member IDs must start at 1");
                            }
                            if (field.ConstantValue > definition.Fields.Count)
                            {
                                throw new InvalidFieldException(field, "Message ID is greater than field count");
                            }
                            break;
                    }
                }
            }
        }
    }
}
