using System;
using System.Collections.Generic;
using System.Linq;
using Core.Exceptions;
using Core.Meta.Interfaces;

namespace Core.Meta
{
    /// <inheritdoc/>
    public struct BebopSchema : ISchema
    {
        public BebopSchema(string nameSpace, Dictionary<string, Definition> definitions)
        {
            Namespace = nameSpace;
            Definitions = definitions;
            _sortedDefinitions = null;
        }
        /// <inheritdoc/>
        public string Namespace { get; }
        /// <inheritdoc/>
        public Dictionary<string, Definition> Definitions { get; }

        /// <summary>
        /// A cached result of SortedDefinitions.
        /// </summary>
        private List<Definition>? _sortedDefinitions;

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
                    var values = new HashSet<uint>();
                    var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var field in ed.Members)
                    {
                        if (values.Contains(field.ConstantValue))
                        {
                            throw new InvalidFieldException(field, "Enum value must be unique");
                        }
                        if (names.Contains(field.Name))
                        {
                            throw new DuplicateFieldException(field, ed);
                        }
                        values.Add(field.ConstantValue);
                        names.Add(field.Name);
                    }
                }
            }
        }
    }
}
