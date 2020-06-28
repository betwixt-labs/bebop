using System.Collections.Generic;
using System.Linq;
using Compiler.Exceptions;
using Compiler.Meta.Interfaces;

namespace Compiler.Meta
{
    /// <inheritdoc/>
    public readonly struct PierogiSchema : ISchema
    {
        public PierogiSchema(string sourceFile, string package, ICollection<IDefinition> definitions)
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
        public ICollection<IDefinition> Definitions { get; }

       
        /// <inheritdoc/>
        public void Validate()
        {
            
            foreach (var definition in Definitions)
            {
                if (Definitions.Count(d => d.Name.Equals(definition.Name)) > 1)
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
                        if (field.ConstantValue < 0)
                        {
                            throw FailFast.InvalidField(field, "may only contain positive field ids", SourceFile);
                        }
                        if (definition.Fields.Count(f => f.ConstantValue == field.ConstantValue) > 1)
                        {
                            throw FailFast.InvalidField(field, "contains duplicate field ids", SourceFile);
                        }
                       
                        if (field.ConstantValue > definition.Fields.Count)
                        {
                            throw FailFast.InvalidField(field, "contains a field id that is greater than the total definition members", SourceFile);
                        }
                    }
                    // check for nested struct definitions 
                    if (definition.Kind == AggregateKind.Struct && !field.IsArray && field.TypeCode > -1 && definition.Name.Equals(Definitions.ElementAt(field.TypeCode).Name))
                    {
                        throw FailFast.RecursiveException(definition, SourceFile);
                    }
                }
            }
        }
    }
}
