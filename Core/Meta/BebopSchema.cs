using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Core.Exceptions;
using Core.Lexer.Tokenization.Models;
using Core.Meta.Interfaces;

namespace Core.Meta
{
    /// <inheritdoc/>
    public struct BebopSchema : ISchema
    {
        private List<SpanException> _parsingErrors;
        private List<SpanException> _validationErrors;
        private List<SpanException> _validationWarnings;
        public List<SpanException> Errors => _parsingErrors.Concat(_validationErrors).ToList();
        public List<SpanException> Warnings => _validationWarnings;

        public BebopSchema(string nameSpace, Dictionary<string, Definition> definitions, HashSet<(Token, Token)> typeReferences, List<SpanException>? parsingErrors = null)
        {
            Namespace = nameSpace;
            Definitions = definitions;
            _sortedDefinitions = null;
            _validationErrors = new();
            _validationWarnings = new();
            _parsingErrors = parsingErrors ?? new();
            _typeReferences = typeReferences;
        }
        /// <inheritdoc/>
        public string Namespace { get; }
        /// <inheritdoc/>
        public Dictionary<string, Definition> Definitions { get; }

        /// <summary>
        /// A cached result of SortedDefinitions.
        /// </summary>
        private List<Definition>? _sortedDefinitions;

        private HashSet<(Token, Token)> _typeReferences;

        /// <inheritdoc/>
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
                if (deps.Count() == 0)
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

        /// <inheritdoc/>
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
                if (ReservedWords.Identifiers.Contains(definition.Name))
                {
                    errors.Add(new ReservedIdentifierException(definition.Name, definition.Span));
                }
                if (definition is TopLevelDefinition td && td.OpcodeAttribute != null)
                {
                    if (!td.OpcodeAttribute.TryValidate(out var opcodeReason))
                    {
                        errors.Add(new InvalidOpcodeAttributeValueException(td, opcodeReason));
                    }
                    if (Definitions.Values.Count(d => d is TopLevelDefinition td2 && td2.OpcodeAttribute != null && td2.OpcodeAttribute.Value.Equals(td.OpcodeAttribute.Value)) > 1)
                    {
                        errors.Add(new DuplicateOpcodeException(td));
                    }
                }
                if (definition is FieldsDefinition fd) foreach (var field in fd.Fields)
                {
                    if (ReservedWords.Identifiers.Contains(field.Name))
                    {
                        errors.Add(new ReservedIdentifierException(field.Name, field.Span));
                    }
                    if (field.DeprecatedAttribute != null && fd is StructDefinition)
                    {
                        errors.Add(new InvalidDeprecatedAttributeUsageException(field));
                    }
                    if (fd.Fields.Count(f => f.Name.Equals(field.Name, StringComparison.OrdinalIgnoreCase)) > 1)
                    {
                        errors.Add(new DuplicateFieldException(field, fd));
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
                        case MessageDefinition when field.ConstantValue > fd.Fields.Count:
                        {
                            errors.Add(new InvalidFieldException(field, "Message index is greater than field count"));
                            break;
                        }
                        default:
                            break;
                    }
                }
                if (definition is EnumDefinition ed)
                {
                    var values = new HashSet<BigInteger>();
                    var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var field in ed.Members)
                    {
                        if (!ed.IsBitFlags && values.Contains(field.ConstantValue))
                        {
                            errors.Add(new InvalidFieldException(field, "Enum value must be unique if the enum is not a [flags] enum"));
                        }
                        if (names.Contains(field.Name))
                        {
                            errors.Add(new DuplicateFieldException(field, ed));
                        }
                        if (!BaseTypeHelpers.InRange(ed.BaseType, field.ConstantValue))
                        {
                            var min = BaseTypeHelpers.MinimumInteger(ed.BaseType);
                            var max = BaseTypeHelpers.MaximumInteger(ed.BaseType);
                            errors.Add(new InvalidFieldException(field,
                                $"Enum value {field.ConstantValue} of {field.Name} is outside the range for underlying type {ed.BaseType}. " +
                                $"Valid values are in the range [{min}, {max}]."));
                        }
                        values.Add(field.ConstantValue);
                        names.Add(field.Name);
                    }
                }
            }
            _validationErrors = errors;
            return errors;
        }
    }
}
