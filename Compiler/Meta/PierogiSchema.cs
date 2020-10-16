using System.Collections.Generic;
using System.Linq;
using Compiler.Exceptions;
using Compiler.Meta.Interfaces;

namespace Compiler.Meta
{
    /// <inheritdoc/>
    public readonly struct PierogiSchema : ISchema
    {
        public PierogiSchema(string sourcePath, string package, Dictionary<string, IDefinition> definitions)
        {
            SourcePath = sourcePath;
            Package = package;
            Definitions = definitions;
        }
        /// <inheritdoc/>
        public string SourcePath { get; }
        /// <inheritdoc/>
        public string Package { get; }
        /// <inheritdoc/>
        public Dictionary<string, IDefinition> Definitions { get; }


        /// <inheritdoc/>
        public void Validate()
        {

            foreach (var definition in Definitions.Values)
            {
                if (Definitions.Values.Count(d => d.Name.Equals(definition.Name)) > 1)
                {
                    throw new MultipleDefinitionsException(definition, SourcePath);
                }
                if (ReservedWords.Identifiers.Contains(definition.Name))
                {
                    throw new ReservedIdentifierException(definition.Name, definition.Span, SourcePath);
                }
                if (definition.IsReadOnly && !definition.IsStruct())
                {
                    throw new InvalidReadOnlyException(definition, SourcePath);
                }
                foreach (var field in definition.Fields)
                {
                    if (ReservedWords.Identifiers.Contains(field.Name))
                    {
                        throw new ReservedIdentifierException(field.Name, field.Span, SourcePath);
                    }
                    if (field.DeprecatedAttribute.HasValue && !definition.IsMessage())
                    {
                        throw new InvalidDeprectedAttributeException(field, SourcePath);
                    }
                    switch (definition.Kind)
                    {
                        case AggregateKind.Enum:
                            if (field.ConstantValue < 0)
                            {
                                throw new InvalidFieldException(field, "Enum values must start at 0", SourcePath);
                            }
                            if (definition.Fields.Count(f => f.ConstantValue == field.ConstantValue) > 1)
                            {
                                throw new InvalidFieldException(field, "Enum value must be unique", SourcePath);
                            }
                            break;
                        case AggregateKind.Struct:
                            if (field.Type is DefinedType dt && definition.Name.Equals(dt.Name))
                            {
                                throw new InvalidFieldException(field, "Struct contains itself", SourcePath);
                            }
                            break;
                        case AggregateKind.Message:
                            if (definition.Fields.Count(f => f.ConstantValue == field.ConstantValue) > 1)
                            {
                                throw new InvalidFieldException(field, "Message ID must be unique", SourcePath);
                            }
                            if (field.ConstantValue <= 0)
                            {
                                throw new InvalidFieldException(field, "Message member IDs must start at 1", SourcePath);
                            }
                            if (field.ConstantValue > definition.Fields.Count)
                            {
                                throw new InvalidFieldException(field, "Message ID is greater than field count", SourcePath);
                            }
                            break;
                    }
                }
            }
        }
    }
}
