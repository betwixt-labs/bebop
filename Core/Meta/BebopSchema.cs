using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Core.Exceptions;
using Core.IO;
using Core.Lexer.Tokenization.Models;
using Core.Meta.Decorators;
using Core.Meta.Extensions;
using Core.Parser;
using Core.Parser.Extensions;

namespace Core.Meta
{
    /// <summary>
    /// Represents the contents of a textual Bebop schema 
    /// </summary>
    public struct BebopSchema
    {
        private static string[] _enumZeroNames = new[] { "Default", "Unknown", "Invalid", "Null", "None", "Zero", "False" };
        private List<SpanException> _parsingErrors;
        private List<SpanException> _parsingWarnings;
        private List<SpanException> _validationErrors;
        private List<SpanException> _validationWarnings;

        /// <summary>
        /// Errors found while validating this schema.
        /// </summary>
        public List<SpanException> Errors => _parsingErrors.Concat(_validationErrors).ToList();
        public List<SpanException> Warnings => _parsingWarnings.Concat(_validationWarnings).ToList();

        public List<string> Imports { get; }

        public BebopSchema(Dictionary<string, Definition> definitions, HashSet<(Token, Token)> typeReferences, List<SpanException>? parsingErrors = null, List<SpanException>? parsingWarnings = null, List<string>? imports = null)
        {
            Definitions = definitions;
            Imports = imports ?? new List<string>();

            _sortedDefinitions = null;
            _validationErrors = new();
            _validationWarnings = new();
            _parsingErrors = parsingErrors ?? new();
            _parsingWarnings = parsingWarnings ?? new();
            _typeReferences = typeReferences;
        }

        /// <summary>
        /// All Bebop definitions in this schema, keyed by their name.
        /// </summary>
        public Dictionary<string, Definition> Definitions { get; }

        /// <summary>
        /// A cached result of SortedDefinitions.
        /// </summary>
        private List<Definition>? _sortedDefinitions;

        private HashSet<(Token, Token)> _typeReferences;

        /// <summary>
        /// A topologically sorted list of definitions.
        /// </summary>
        public List<Definition> SortedDefinitions()
        {
            // Return the cached result if it exists.
            if (_sortedDefinitions != null) return _sortedDefinitions;

            // https://en.wikipedia.org/w/index.php?title=Topological_sorting&oldid=1011036708#Kahn%27s_algorithm
            // We keep track of node in-degrees and an adjacency list.
            var in_degree = new Dictionary<string, int>();
            var graph = new Dictionary<string, List<string>>();
            foreach (var kv in Definitions)
            {
                var name = kv.Key;
                graph[name] = new List<string>();
                in_degree[name] = 0;
            }

            // Populate graph / in_degree and find starting nodes (for set S in the algorithm).
            var nextQueue = new Queue<string>();
            foreach (var kv in Definitions)
            {
                var name = kv.Key;
                var definition = kv.Value;
                var deps = definition.Dependencies();
                if (!deps.Any())
                {
                    nextQueue.Enqueue(name);
                }
                foreach (var dep in deps)
                {
                    graph[dep].Add(name);
                    in_degree[name] += 1;
                }
            }

            // Now run the algorithm.
            var sortedList = new List<Definition>();
            while (nextQueue.Count > 0)
            {
                var name = nextQueue.Dequeue();
                sortedList.Add(Definitions[name]);
                foreach (var dependent in graph[name])
                {
                    in_degree[dependent] -= 1;
                    if (in_degree[dependent] == 0)
                    {
                        nextQueue.Enqueue(dependent);
                    }
                }
            }

            var cycle = in_degree.FirstOrDefault(kv => kv.Value > 0);
            if (cycle.Key != null)
            {
                throw new CyclicDefinitionsException(Definitions[cycle.Key]);
            }

            _sortedDefinitions = sortedList;
            return _sortedDefinitions;
        }

        private readonly List<SpanException> ValidateDefinitionDecorators(List<SchemaDecorator> decorators, Definition definition)
        {
            var validationErrors = new List<SpanException>();
            foreach (var decorator in decorators)
            {
                if (!decorator.IsUsableOn())
                {
                    validationErrors.Add(new InvalidDecoratorUsageException(decorator.Identifier, $"Decorator '{decorator.Identifier}' cannot be applied to {definition.Name.ToLowerInvariant()} '{definition.Name}'", decorator.Span));
                }
                if (!decorator.Definition.TryValidate(out var reason, decorator))
                {
                    if (decorator.Identifier == "opcode")
                    {
                        validationErrors.Add(new InvalidOpcodeDecoratorValueException(definition, reason));
                    }
                    else
                    {
                        validationErrors.Add(new DecoratorValidationException(decorator.Identifier, reason, decorator.Span));
                    }
                }
                if (decorator.Identifier == "opcode")
                {
                    if (definition is RecordDefinition td && td.OpcodeDecorator is not null)
                    {
                        if (Definitions.Values.Count(d => d is RecordDefinition td2 && td2.OpcodeDecorator is not null && td2.OpcodeDecorator.Arguments["fourcc"].Equals(td.OpcodeDecorator.Arguments["fourcc"])) > 1)
                        {
                            validationErrors.Add(new DuplicateOpcodeException(td));
                        }
                    }
                }
                if (!decorator.Definition.AllowMultiple && decorators.Count(a => a.Identifier == decorator.Identifier) > 1)
                {
                    validationErrors.Add(new MultipleDecoratorsException(decorator.Identifier, decorator.Span));
                }
            }
            return validationErrors;
        }

        private static List<SpanException> ValidateFieldDecorators(List<SchemaDecorator> decorators, Field field, Definition parent)
        {
            var validationErrors = new List<SpanException>();
            foreach (var decorator in decorators)
            {

                if (!decorator.IsUsableOn())
                {
                    var hint = parent is StructDefinition && decorator.Identifier == "deprecated" ? "deprecated decorator cannot be applied to struct fields" : "";
                    validationErrors.Add(new InvalidDecoratorUsageException(decorator.Identifier, $"Decorator '{decorator.Identifier}' cannot be applied to '{parent.Name}.{field.Name}'", decorator.Span, hint));
                }
                if (!decorator.Definition.TryValidate(out var reason, decorator))
                {
                    validationErrors.Add(new DecoratorValidationException(decorator.Identifier, reason, decorator.Span));
                }
                if (!decorator.Definition.AllowMultiple && decorators.Count(a => a.Identifier == decorator.Identifier) > 1)
                {
                    validationErrors.Add(new MultipleDecoratorsException(decorator.Identifier, decorator.Span));
                }
            }
            return validationErrors;
        }

        /// <summary>
        /// Validates that the schema is made up of well-formed values.
        /// </summary>
        public List<SpanException> Validate()
        {
            var errors = new List<SpanException>();
            foreach (var (typeToken, definitionToken) in _typeReferences)
            {
                if (!Definitions.ContainsKey(typeToken.Lexeme))
                {
                    errors.Add(new UnrecognizedTypeException(typeToken, definitionToken.Lexeme));
                    continue;
                }
                var reference = Definitions[typeToken.Lexeme];
                var referenceScope = reference.Scope;
                // TODO figure out why this is happening
                //  it is obvious that the ServiceDefinition isn't being added
                //  but it remains to be seen why it is still being picked up by _typeReferences
                if (!Definitions.ContainsKey(definitionToken.Lexeme))
                {
                    //   errors.Add(new UnrecognizedTypeException(definitionToken, typeToken.Lexeme));
                    continue;
                }
                var definition = Definitions[definitionToken.Lexeme];
                var definitionScope = definition.Scope;

                // You're not allowed to reference types declared within a union from elsewhere
                // Check if reference has a union in scope but definition does not have the same union in scope
                // Throw ReferenceScopeException if so
                if (referenceScope.Find((parent) => parent is UnionDefinition) is UnionDefinition union)
                {
                    if (!definitionScope.Contains(union))
                    {
                        errors.Add(new ReferenceScopeException(definition, reference, "union"));
                    }
                }
            }
            foreach (var definition in Definitions.Values)
            {
                if (Definitions.Values.Count(d => d.Name.Equals(definition.Name, StringComparison.OrdinalIgnoreCase)) > 1)
                {
                    errors.Add(new MultipleDefinitionsException(definition));
                }
                if (ReservedWords.Identifiers.Contains(definition.Name, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add(new ReservedIdentifierException(definition.Name, definition.Span));
                }
                if (definition is FieldsDefinition fd)
                {
                    var fieldDefinitionDecorators = fd.Decorators;
                    if (fieldDefinitionDecorators.Count is > 0)
                    {
                        errors.AddRange(ValidateDefinitionDecorators(fieldDefinitionDecorators, fd));
                    }
                    if (fd.Fields.Count > byte.MaxValue)
                    {
                        errors.Add(new StackSizeExceededException("A definition cannot have more than 255 fields", fd.Span));
                    }
                    foreach (var field in fd.Fields)
                    {
                        var fieldDecorators = field.Decorators;
                        if (fieldDecorators.Count is > 0)
                        {
                            errors.AddRange(ValidateFieldDecorators(fieldDecorators, field, fd));
                        }
                        if (ReservedWords.Identifiers.Contains(field.Name))
                        {
                            errors.Add(new ReservedIdentifierException(field.Name, field.Span));
                        }
                        if (fd.Fields.Count(f => f.Name.Equals(field.Name, StringComparison.OrdinalIgnoreCase)) > 1)
                        {
                            errors.Add(new DuplicateFieldException(field, fd));
                        }
                        if (field.Type is DefinedType st && Definitions.TryGetValue(st.Name, out var std) && std is ServiceDefinition)
                        {
                            errors.Add(new InvalidFieldException(field, "Field cannot be a service"));
                        }
                        switch (fd)
                        {
                            case StructDefinition when field.Type is DefinedType dt && fd.Name.Equals(dt.Name):
                                {
                                    errors.Add(new InvalidFieldException(field, "Struct contains itself"));
                                    break;
                                }
                            case MessageDefinition when fd.Fields.Count(f => f.ConstantValue == field.ConstantValue) > 1:
                                {
                                    errors.Add(new InvalidFieldException(field, "Message index must be unique"));
                                    break;
                                }
                            case MessageDefinition when field.ConstantValue <= 0:
                                {
                                    errors.Add(new InvalidFieldException(field, "Message member index must start at 1"));
                                    break;
                                }
                            case MessageDefinition when field.ConstantValue > byte.MaxValue:
                                {
                                    errors.Add(new InvalidFieldException(field, "Message index must be less than or equal to 255"));
                                    break;
                                }
                            case MessageDefinition when field.ConstantValue > fd.Fields.Count:
                                {
                                    errors.Add(new InvalidFieldException(field, "Message index is greater than field count"));
                                    break;
                                }
                            default:
                                break;
                        }
                    }
                }
                if (definition is ServiceDefinition sd)
                {
                    var serviceDecorators = sd.Decorators;
                    if (serviceDecorators.Count is > 0)
                    {
                        errors.AddRange(ValidateDefinitionDecorators(serviceDecorators, sd));
                    }
                    var usedMethodNames = new HashSet<string>();
                    var usedMethodIds = new HashSet<uint>();
                    if (sd.Methods.Count > byte.MaxValue)
                    {
                        errors.Add(new StackSizeExceededException("A service cannot have more than 255 methods", sd.Span));
                    }
                    foreach (var b in sd.Methods)
                    {

                        var fnd = b.Definition;
                        var methodDecorators = b.Decorators;
                        if (methodDecorators.Count is > 0)
                        {
                            errors.AddRange(ValidateDefinitionDecorators(methodDecorators, fnd));
                        }
                        if (!usedMethodNames.Add(fnd.Name.ToSnakeCase()))
                        {
                            errors.Add(new DuplicateServiceMethodNameException(sd.Name, fnd.Name, fnd.Span));
                        }
                        if (!usedMethodIds.Add(b.Id))
                        {
                            errors.Add(new DuplicateServiceMethodIdException(b.Id, sd.Name, fnd.Name, fnd.Span));
                        }
                        if (fnd.RequestDefinition.IsDefined(this) && !fnd.RequestDefinition.IsAggregate(this))
                        {
                            errors.Add(new InvalidServiceRequestTypeException(sd.Name, fnd.Name, fnd.RequestDefinition, fnd.Span));
                        }
                        if (fnd.ResponseDefintion.IsDefined(this) && !fnd.ResponseDefintion.IsAggregate(this))
                        {
                            errors.Add(new InvalidServiceReturnTypeException(sd.Name, fnd.Name, fnd.ResponseDefintion, fnd.Span));
                        }

                        if (fnd.Parent != sd)
                        {
                            throw new Exception("A function was registered to multiple services, this is an error in bebop core.");
                        }
                    }
                }
                if (definition is EnumDefinition ed)
                {
                    var enumDecorators = ed.Decorators;
                    if (enumDecorators.Count is > 0)
                    {
                        errors.AddRange(ValidateDefinitionDecorators(enumDecorators, ed));
                    }
                    var values = new HashSet<BigInteger>();
                    var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (ed.Members.Count > byte.MaxValue)
                    {
                        errors.Add(new StackSizeExceededException("An enum cannot have more than 255 members", ed.Span));
                    }
                    foreach (var field in ed.Members)
                    {

                        var fieldDecorators = field.Decorators;
                        if (fieldDecorators.Count is > 0)
                        {
                            errors.AddRange(ValidateFieldDecorators(fieldDecorators, field, ed));
                        }
                        if (!ed.IsBitFlags && values.Contains(field.ConstantValue))
                        {
                            errors.Add(new InvalidFieldException(field, "Enum value must be unique if the enum is not a [flags] enum"));
                        }
                        if (names.Contains(field.Name))
                        {
                            errors.Add(new DuplicateFieldException(field, ed));
                        }
                        if (!ed.BaseType.CanRepresent(field.ConstantValue))
                        {
                            var min = BaseTypeHelpers.MinimumInteger(ed.BaseType);
                            var max = BaseTypeHelpers.MaximumInteger(ed.BaseType);
                            errors.Add(new InvalidFieldException(field,
                                $"Enum member '{field.Name}' has a value ({field.ConstantValue}) that " +
                                $"is outside the domain of underlying type '{ed.BaseType.BebopName()}'. " +
                                $"Valid values range from {min} to {max}."));
                        }
                        if (field.ConstantValue == 0 && !_enumZeroNames.Any(x => x.Equals(field.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            _validationWarnings.Add(new EnumZeroWarning(field));
                        }
                        values.Add(field.ConstantValue);
                        names.Add(field.Name);
                    }
                }
                if (definition is UnionDefinition ud)
                {
                    var unionDecorators = ud.Decorators;
                    if (unionDecorators.Count is > 0)
                    {
                        errors.AddRange(ValidateDefinitionDecorators(unionDecorators, ud));
                    }
                    if (ud.Branches.Count > byte.MaxValue)
                    {
                        errors.Add(new StackSizeExceededException("A union cannot have more than 255 members", ud.Span));
                    }
                }
            }
            var methodIds = new Dictionary<uint, (string serviceName, string methodName)>();

            foreach (var service in Definitions.Values.OfType<ServiceDefinition>())
            {
                foreach (var method in service.Methods)
                {
                    if (methodIds.ContainsKey(method.Id))
                    {
                        var (firstServiceName, firstMethodName) = methodIds[method.Id];
                        errors.Add(new ServiceMethodIdCollisionException(firstServiceName, firstMethodName, service.Name, method.Definition.Name, method.Id, service.Span));
                    }
                }
            }
            _validationErrors = errors;
            return errors;
        }

        public readonly byte[] ToBinary()
        {
            using var writer = new BinarySchemaWriter(this);
            return writer.Encode();
        }
    }
}
