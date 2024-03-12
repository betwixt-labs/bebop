using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Lexer.Tokenization.Models;
using Core.Meta.Decorators;
using Core.Parser;

namespace Core.Meta
{
    /// <summary>
    /// A base class for definitions in a schema.
    /// </summary>
    public abstract class Definition
    {
        protected Definition(string name, Span span, string documentation, Definition? parent = null)
        {
            Parent = parent;
            Name = name;
            Span = span;
            Documentation = documentation;
        }

        /// <summary>
        /// The name of the current definition.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     The span where the definition was found.
        /// </summary>
        public Span Span { get; }

        /// <summary>
        /// The inner text of a block comment that preceded the definition.
        /// </summary>
        public string Documentation { get; set; }

        /// <summary>
        /// The names of types this definition depends on / refers to.
        /// </summary>
        public abstract IEnumerable<string> Dependencies();

        /// <summary>
        /// Immediate parent of this definition, if it is enclosed in another definition.
        /// </summary>
        public Definition? Parent { get; set; }

        /// <summary>
        /// List of definitions enclosing this definition, from outer to inner. Empty if this is a top level definition.
        /// </summary>
        public List<Definition> Scope
        {
            get
            {
                var scope = new List<Definition>();
                var currentDefinition = this;
                while (currentDefinition.Parent is not null)
                {
                    scope.Insert(0, currentDefinition.Parent);
                    currentDefinition = currentDefinition.Parent;
                }
                return scope;
            }
        }
    }

    /// <summary>
    /// A base class for definitions that can have an opcode, and are therefore valid at the "top level" of a Bebop packet.
    /// (In other words: struct, message, union. But you can't send a raw enum over the wire or a service.)
    /// </summary>
    public abstract class RecordDefinition : Definition
    {
        protected RecordDefinition(string name, Span span, string documentation, List<SchemaDecorator> decorators, Definition? parent = null) :
            base(name, span, documentation, parent)
        {
            Decorators = decorators;
        }

        public SchemaDecorator? OpcodeDecorator => Decorators?.FirstOrDefault((a) => a.Identifier == "opcode");

        public SchemaDecorator? DeprecatedDecorator => Decorators?.FirstOrDefault((a) => a.Identifier == "deprecated");

        public List<SchemaDecorator> Decorators { get; }

        /// <summary>
        /// If this definition is part of a union branch, then this is its discriminator in the parent union.
        /// Otherwise, this property is null. (This feels a bit hacky, but oh well.)
        /// </summary>
        public byte? DiscriminatorInParent { get; set; }

        /// <summary>
        /// Compute a lower bound for the size of the wire-format encoding of a packet conforming to this definition.
        /// </summary>
        /// <param name="schema">The schema this definition belongs to, used to resolve references to other definitions.</param>
        /// <returns>The lower bound, in bytes.</returns>
        public abstract int MinimalEncodedSize(BebopSchema schema);
    }

    /// <summary>
    /// A base class for definitions that are an aggregate of fields. (struct, message)
    /// </summary>
    public abstract class FieldsDefinition : RecordDefinition
    {
        protected FieldsDefinition(string name, Span span, string documentation, List<SchemaDecorator> decorators, ICollection<Field> fields, Definition? parent = null) :
            base(name, span, documentation, decorators, parent)
        {
            Fields = fields;
        }

        public ICollection<Field> Fields { get; }

        public override IEnumerable<string> Dependencies() =>
            Fields.SelectMany(field => field.Type.Dependencies()).Distinct();
    }

    /// <summary>
    /// A class representing a struct definition.
    /// 
    /// A struct is an aggregate of some fields that are always present in a fixed order. It promises to never grow in later versions of the schema.
    /// </summary>
    public class StructDefinition : FieldsDefinition
    {
        public StructDefinition(string name, Span span, string documentation, List<SchemaDecorator> decorators, ICollection<Field> fields, bool isMutable, Definition? parent = null) :
            base(name, span, documentation, decorators, fields, parent)
        {
            IsMutable = isMutable;
        }

        /// <summary>
        /// Is this struct "read-only"? (This will mean something like: not generating setters in the codegen.)
        /// </summary>
        public bool IsMutable { get; }

        override public int MinimalEncodedSize(BebopSchema schema)
        {
            // The encoding of a struct consists of a straightforward concatenation of the encodings of its fields.
            return Fields.Sum(f => f.MinimalEncodedSize(schema));
        }

        /// <summary>
        /// Checks whether this struct is always going to serialize to the exact same size. This means it must only be
        /// composed of primitives (non-strings), enums, and other fixed-sized structs at present.
        /// </summary>
        /// <param name="definitions">Other definitions of defined types in the schema that need to get referenced if
        /// this struct contains any.</param>
        public bool IsFixedSize(Dictionary<string, Definition> definitions) => Fields.All((f) =>
            f.Type switch
            {
                DefinedType dt => definitions[dt.Name] switch
                {
                    StructDefinition sd => sd.IsFixedSize(definitions),
                    EnumDefinition ed => true,
                    _ => false
                },
                ScalarType st => st.IsFixedScalar(),
                _ => false
            });
        public bool IsFixedSize(BebopSchema schema) => IsFixedSize(schema.Definitions);
    }

    /// <summary>
    /// A class representing a message definition.
    /// 
    /// A message is an aggregate of optional fields. Each field is prefixed on the wire by its index in the message. A message may grow in a later version of the schema.
    /// </summary>
    public class MessageDefinition : FieldsDefinition
    {
        public MessageDefinition(string name, Span span, string documentation, List<SchemaDecorator> decorators, ICollection<Field> fields, Definition? parent = null) : base(name, span, documentation, decorators, fields, parent)
        {
        }

        override public int MinimalEncodedSize(BebopSchema schema)
        {
            // If all fields are absent.
            return 5;
        }
    }

    /// <summary>
    /// Represents an enum definition in a schema.
    /// </summary>
    public class EnumDefinition : Definition
    {
        public EnumDefinition(
            string name,
            Span span,
            string documentation,
            ICollection<Field> members,
            List<SchemaDecorator> decorators,
            BaseType baseType,
            Definition? parent = null
        ) : base(name, span, documentation, parent)
        {
            Members = members;
            Decorators = decorators;
            IsBitFlags = decorators?.Any((a) => a.Identifier == "flags") ?? false;
            BaseType = baseType;
        }

        public ICollection<Field> Members { get; }

        public bool IsBitFlags { get; }

        public BaseType BaseType { get; }

        public override IEnumerable<string> Dependencies() => Enumerable.Empty<string>();

        public ScalarType ScalarType => new ScalarType(BaseType);

        public List<SchemaDecorator> Decorators { get; }

        public SchemaDecorator? DeprecatedDecorator => Decorators?.FirstOrDefault((a) => a.Identifier == "deprecated");
    }

    public class UnionBranch
    {
        public readonly byte Discriminator;
        public readonly RecordDefinition Definition;

        public UnionBranch(byte discriminator, RecordDefinition definition)
        {
            Discriminator = discriminator;
            Definition = definition;
        }
    }

    public class ServiceMethod
    {
        public readonly string Documentation;
        public readonly uint Id;
        public readonly MethodDefinition Definition;

        public SchemaDecorator? DeprecatedDecorator => Decorators?.FirstOrDefault((a) => a.Identifier == "deprecated");
        public List<SchemaDecorator> Decorators { get; }

        public ServiceMethod(uint id, MethodDefinition definition, string documentation, List<SchemaDecorator> decorators)
        {
            Id = id;
            Definition = definition;
            Documentation = documentation;
            Decorators = decorators;
        }
    }

    public class UnionDefinition : RecordDefinition
    {
        public UnionDefinition(string name, Span span, string documentation, List<SchemaDecorator> decorators, ICollection<UnionBranch> branches, Definition? parent = null) : base(name, span, documentation, decorators, parent)
        {
            Branches = branches;
        }

        public ICollection<UnionBranch> Branches { get; }

        public override IEnumerable<string> Dependencies() => Branches.Select(b => b.Definition.Name);

        override public int MinimalEncodedSize(BebopSchema schema)
        {
            // Length + discriminator + shortest branch.
            return 4 + 1 + (Branches.Count == 0 ? 0 : Branches.Min(b => b.Definition.MinimalEncodedSize(schema)));
        }
    }

    public class ServiceDefinition : Definition
    {


        public ServiceDefinition(string name, Span span, string documentation, ICollection<ServiceMethod> methods, List<SchemaDecorator> decorators) : base(name, span, documentation)
        {
            foreach (var m in methods)
            {
                m.Definition.Parent = this;
            }
            Methods = methods;
            Decorators = decorators;
        }
        public List<SchemaDecorator> Decorators { get; }
        public SchemaDecorator? DeprecatedDecorator => Decorators?.FirstOrDefault((a) => a.Identifier == "deprecated");
        public ICollection<ServiceMethod> Methods { get; }

        public override IEnumerable<string> Dependencies() => Methods.SelectMany(f => f.Definition.Dependencies());

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Service: {Name}");
            builder.AppendLine("Methods ->");
            foreach (var method in Methods)
            {
                builder.AppendLine($"    {method.Definition.Name}({method.Definition.RequestDefinition}): {method.Definition.ResponseDefintion} ({method.Id})");
            }
            return builder.ToString();
        }
    }

    /// <summary>
    /// Methods at this time are only stored within service branches and not globally.
    /// </summary>
    public class MethodDefinition : Definition
    {
        public MethodDefinition(string name, Span span, string documentation, TypeBase requestDefinition, TypeBase responseDefinition, MethodType methodType, Definition? parent = null)
            : base(name, span, documentation, parent)
        {
            RequestDefinition = requestDefinition;
            ResponseDefintion = responseDefinition;
            Type = methodType;
        }

        public TypeBase RequestDefinition { get; }
        public TypeBase ResponseDefintion { get; }
        public MethodType Type { get; }

        public override IEnumerable<string> Dependencies() =>
            RequestDefinition.Dependencies().Concat(ResponseDefintion.Dependencies());
    }

    public class ConstDefinition : Definition
    {
        public ConstDefinition(string name, Span span, string documentation, Literal value, Definition? parent = null) : base(name, span, documentation, parent)
        {
            Value = value;
        }

        public override IEnumerable<string> Dependencies() => Enumerable.Empty<string>();

        public Literal Value { get; }
    }
}
