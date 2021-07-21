// unset

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Meta;
using Core.Meta.Extensions;
using Core.Meta.Interfaces;

namespace Core.Generators.Rust
{
    enum OwnershipType
    {
        Borrowed,
        Owned,
        Constant,
    }

    public class RustGenerator : BaseGenerator
    {
        const int indentStep = 4;

        private static readonly string[] _reservedWordsArray =
        {
            "Self", "abstract", "as", "bebop", "become", "box", "break", "const", "continue", "crate", "do", "else",
            "enum", "extern", "false", "final", "fn", "for", "if", "impl", "in", "let", "loop", "macro", "match",
            "mod", "move", "mut", "override", "priv", "pub", "ref", "return", "self", "static", "std", "struct",
            "super", "trait", "true", "try", "type", "typeof", "unsafe", "unsized", "use", "virtual", "where",
            "while", "yield",
        };

        private static readonly HashSet<string> _reservedWords = RustGenerator._reservedWordsArray.ToHashSet();

        public RustGenerator(ISchema schema) : base(schema) { }

        /// <summary>
        /// Generate a Rust type name for the given <see cref="TypeBase"/>.
        /// </summary>
        /// <param name="type">The field type to generate code for.</param>
        /// <param name="ot">Ownership type, e.g. <c>&'raw str</c> versus <c>String</c>.</param>
        /// <returns>The Rust type name.</returns>
        private string TypeName(in TypeBase type, OwnershipType ot = OwnershipType.Borrowed)
        {
            switch (type)
            {
                case ScalarType st:
                    return st.BaseType switch
                    {
                        BaseType.Bool => "bool",
                        BaseType.Byte => "u8",
                        BaseType.UInt16 => "u16",
                        BaseType.Int16 => "i16",
                        BaseType.UInt32 => "u32",
                        BaseType.Int32 => "i32",
                        BaseType.UInt64 => "u64",
                        BaseType.Int64 => "i64",
                        BaseType.Float32 => "f32",
                        BaseType.Float64 => "f64",
                        BaseType.String => ot switch
                        {
                            OwnershipType.Borrowed => "&'raw str",
                            OwnershipType.Constant => "&str",
                            OwnershipType.Owned => "String",
                            _ => throw new ArgumentOutOfRangeException(nameof(ot))
                        },
                        BaseType.Guid => "bebop::Guid",
                        BaseType.Date => "bebop::Date",
                        _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                    };
                case ArrayType at:
                    if (ot is OwnershipType.Borrowed or OwnershipType.Constant && at.MemberType is ScalarType)
                    {
                        var lifetime = ot is OwnershipType.Borrowed ? "'raw" : "'static";
                        return at.IsOneByteUnits()
                            ? $"&{lifetime} [{TypeName(at.MemberType)}]"
                            : $"bebop::PrimitiveMultiByteArray<{lifetime}, {TypeName(at.MemberType)}>";
                    }
                    else
                    {
                        return $"std::vec::Vec<{TypeName(at.MemberType)}>";
                    }
                case MapType mt:
                    return $"std::collections::HashMap<{TypeName(mt.KeyType)}, {TypeName(mt.ValueType)}>";
                case DefinedType dt:
                    return dt.Name;
            }

            throw new InvalidOperationException($"GetTypeName: {type}");
        }

        private void WriteDocumentation(ref IndentedStringBuilder builder, string documentation)
        {
            foreach (var line in documentation.GetLines())
            {
                // TODO: make the docs more friendly to rustdoc by formatting as markdown
                builder.AppendLine($"/// {line}");
            }
        }

        private string MakeConstIdent(string ident)
        {
            var reCased = ident.ToSnakeCase().ToUpper();
            return _reservedWords.Contains(reCased)
                ? $"_{reCased}"
                : reCased;
        }

        private string MakeEnumVariantIdent(string ident) => MakeDefIdent(ident);

        private string MakeDefIdent(string ident)
        {
            var reCased = ident.ToPascalCase();
            return _reservedWords.Contains(reCased)
                ? $"_{reCased}"
                : reCased;
        }

        private string MakeAttrIdent(string ident)
        {
            var reCased = ident.ToSnakeCase();
            return _reservedWords.Contains(reCased)
                ? $"_{reCased}"
                : reCased;
        }

        private string MakeStringLiteral(string value) =>
            // rust accepts full UTF-8 strings in code AND even supports newlines
            value.Contains("\"#") ? value.Replace("\\", "\\\\").Replace("\"", "\\\"") : $"r#\"{value}\"#";

        private string MakeGuidLiteral(Guid guid)
        {
            var g = guid.ToString("N").ToCharArray();
            var builder = new StringBuilder();
            builder.Append("bebop::Guid::from_be_bytes([");
            for (var i = 0; i < g.Length; i += 2)
            {
                builder.Append("0x");
                builder.Append(g[i]);
                builder.Append(g[i + 1]);
                builder.Append(',');
            }

            builder.Append("])");
            return builder.ToString();
        }

        private string EmitLiteral(Literal literal) =>
            literal switch
            {
                BoolLiteral bl => bl.Value ? "true" : "false",
                IntegerLiteral il => il.Value,
                FloatLiteral {Value: "inf"} => $"{TypeName(literal.Type)}::INFINITY",
                FloatLiteral {Value: "-inf"} => $"{TypeName(literal.Type)}::NEG_INFINITY",
                FloatLiteral {Value: "nan"} => $"{TypeName(literal.Type)}::NAN",
                FloatLiteral fl => fl.Value.Contains('.') ? fl.Value : fl.Value + '.',
                StringLiteral sl => MakeStringLiteral(sl.Value),
                GuidLiteral gl => MakeGuidLiteral(gl.Value),
                _ => throw new ArgumentOutOfRangeException(literal.ToString()),
            };

        private void WriteConstDefinition(ref IndentedStringBuilder builder, ConstDefinition d)
        {
            builder.AppendLine(
                $"pub const {MakeConstIdent(d.Name)}: {TypeName(d.Value.Type, OwnershipType.Constant)} = {EmitLiteral(d.Value)};");
            builder.AppendLine();
        }

        public override string Compile(Version? languageVersion)
        {
            var builder = new IndentedStringBuilder();
            builder.AppendLine(GeneratorUtils.GetXmlAutoGeneratedNotice());

            // TODO: do we need to do something with the namespace? Probably not since the file is itself a module.

            foreach (var definition in Schema.Definitions.Values)
            {
                WriteDocumentation(ref builder, definition.Documentation);
                switch (definition)
                {
                    case ConstDefinition cd:
                        WriteConstDefinition(ref builder, cd);
                        break;
                    // case EnumDefinition ed:
                    //     throw new NotImplementedException();
                    //     break;
                    // case MessageDefinition md:
                    //     throw new NotImplementedException();
                    //     break;
                    // case StructDefinition sd:
                    //     throw new NotImplementedException();
                    //     break;
                    // case FieldsDefinition fd:
                    //     throw new NotImplementedException();
                    //     break;
                    // case UnionDefinition ud:
                    //     throw new NotImplementedException();
                    //     break;
                    // case TopLevelDefinition tld:
                    //     throw new NotImplementedException();
                    //     break;
                    default:
                        throw new InvalidOperationException($"unsupported definition {nameof(definition)}");
                        break;
                }

                ;
            }

            return builder.ToString();
        }

        public override void WriteAuxiliaryFiles(string outputPath)
        {
            // Nothing to do because the runtime is a cargo package.
        }
    }
}