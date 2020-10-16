using System.Collections.Generic;
using System.Linq;
using Compiler.Exceptions;
using Compiler.Meta.Interfaces;

namespace Compiler.Meta
{
    /// <inheritdoc/>
    public readonly struct PierogiSchema : ISchema
    {
        public PierogiSchema(string sourceFile, string package, Dictionary<string, IDefinition> definitions)
        {
            SourceFile = sourceFile;
            Package = package;
            Definitions = definitions;
        }
        /// <inheritdoc/>
        public string SourceFile { get; }
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
                    throw FailFast.DuplicateException(definition, SourceFile);
                }
                if (ReservedTypes.Identifiers.Contains(definition.Name))
                {
                    throw FailFast.ReservedException(definition, SourceFile);
                }
                foreach (var field in definition.Fields)
                {
                    if (definition.Kind != AggregateKind.Struct)
                    {
                        if (definition.Kind == AggregateKind.Message && field.ConstantValue <= 0)
                        {
                            throw FailFast.InvalidField(field, "Message members must start at 1", SourceFile);
                        }
                        if (definition.Kind == AggregateKind.Enum && field.ConstantValue < 0)
                        {
                            throw FailFast.InvalidField(field, "Enum members must start at 0", SourceFile);
                        }
                        if (definition.Fields.Count(f => f.ConstantValue == field.ConstantValue) > 1)
                        {
                            throw FailFast.InvalidField(field, "contains duplicate field ids", SourceFile);
                        }
                        if (definition.Kind == AggregateKind.Message && field.ConstantValue > definition.Fields.Count)
                        {
                            throw FailFast.InvalidField(field, "contains a field id that is greater than the total definition members", SourceFile);
                        }
                    }
                    // check for nested struct definitions 
                    if (definition.Kind == AggregateKind.Struct && (field.Type is DefinedType) && definition.Name.Equals((field.Type as DefinedType).Name))
                    {
                        throw FailFast.RecursiveException(definition, SourceFile);
                    }
                }
            }
        }
    }
}
