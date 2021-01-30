using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Core.Meta;
using Core.Meta.Extensions;
using Core.Meta.Interfaces;

namespace Core.Generators.CPlusPlus
{
    public class CPlusPlusGenerator : Generator
    {
        const int indentStep = 2;

        public CPlusPlusGenerator(ISchema schema) : base(schema) { }

        private string FormatDocumentation(string documentation, int spaces)
        {
            var builder = new IndentedStringBuilder();
            builder.Indent(spaces);
            foreach (var line in documentation.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                builder.AppendLine($"/// {line}");
            }
            return builder.ToString();
        }

        /// <summary>
        /// Generate the body of the <c>encode</c> function for the given <see cref="IDefinition"/>.
        /// </summary>
        /// <param name="definition">The definition to generate code for.</param>
        /// <returns>The generated CPlusPlus <c>encode</c> function body.</returns>
        public string CompileEncode(IDefinition definition)
        {
            return definition.Kind switch
            {
                AggregateKind.Message => CompileEncodeMessage(definition),
                AggregateKind.Struct => CompileEncodeStruct(definition),
                _ => throw new InvalidOperationException($"invalid CompileEncode kind: {definition.Kind} in {definition}"),
            };
        }

        private string CompileEncodeMessage(IDefinition definition)
        {
            var builder = new IndentedStringBuilder(4);
            builder.AppendLine($"const auto pos = writer.reserveMessageLength();");
            builder.AppendLine($"const auto start = writer.length();");
            foreach (var field in definition.Fields)
            {
                if (field.DeprecatedAttribute != null)
                {
                    continue;
                }
                builder.AppendLine($"if (message.{field.Name}.has_value()) {{");
                builder.AppendLine($"  writer.writeByte({field.ConstantValue});");
                builder.AppendLine($"  {CompileEncodeField(field.Type, $"message.{field.Name}.value()", 0, 1)}");
                builder.AppendLine($"}}");
            }
            builder.AppendLine("writer.writeByte(0);");
            builder.AppendLine("const auto end = writer.length();");
            builder.AppendLine("writer.fillMessageLength(pos, end - start);");
            return builder.ToString();
        }

        private string CompileEncodeStruct(IDefinition definition)
        {
            var builder = new IndentedStringBuilder(4);
            foreach (var field in definition.Fields)
            {
                builder.AppendLine(CompileEncodeField(field.Type, $"message.{field.Name}"));
            }
            return builder.ToString();
        }

        private string CompileEncodeField(TypeBase type, string target, int depth = 0, int indentDepth = 0)
        {
            var tab = new string(' ', indentStep);
            var nl = "\n" + new string(' ', indentDepth * indentStep);
            var i = GeneratorUtils.LoopVariable(depth);
            return type switch
            {
                ArrayType at when at.IsBytes() => $"writer.writeBytes({target});",
                ArrayType at =>
                    $"{{" + nl +
                    $"{tab}const auto length{depth} = {target}.size();" + nl +
                    $"{tab}writer.writeUint32(length{depth});" + nl +
                    $"{tab}for (const auto& {i} : {target}) {{" + nl +
                    $"{tab}{tab}{CompileEncodeField(at.MemberType, i, depth + 1, indentDepth + 2)}" + nl +
                    $"{tab}}}" + nl +
                    $"}}",
                MapType mt =>
                    $"writer.writeUint32({target}.size());" + nl +
                    $"for (const auto& e{depth} : {target}) {{" + nl +
                    $"{tab}{CompileEncodeField(mt.KeyType, $"e{depth}.first", depth + 1, indentDepth + 1)}" + nl +
                    $"{tab}{CompileEncodeField(mt.ValueType, $"e{depth}.second", depth + 1, indentDepth + 1)}" + nl +
                    $"}}",
                ScalarType st => st.BaseType switch
                {
                    BaseType.Bool => $"writer.writeBool({target});",
                    BaseType.Byte => $"writer.writeByte({target});",
                    BaseType.UInt16 => $"writer.writeUint16({target});",
                    BaseType.Int16 => $"writer.writeInt16({target});",
                    BaseType.UInt32 => $"writer.writeUint32({target});",
                    BaseType.Int32 => $"writer.writeInt32({target});",
                    BaseType.UInt64 => $"writer.writeUint64({target});",
                    BaseType.Int64 => $"writer.writeInt64({target});",
                    BaseType.Float32 => $"writer.writeFloat32({target});",
                    BaseType.Float64 => $"writer.writeFloat64({target});",
                    BaseType.String => $"writer.writeString({target});",
                    BaseType.Guid => $"writer.writeGuid({target});",
                    BaseType.Date => $"writer.writeDate({target});",
                    _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                },
                DefinedType dt when Schema.Definitions[dt.Name].Kind == AggregateKind.Enum =>
                    $"writer.writeUint32(static_cast<uint32_t>({target}));",
                DefinedType dt => $"{dt.Name}::encodeInto({target}, writer);",
                _ => throw new InvalidOperationException($"CompileEncodeField: {type}")
            };
        }

        /// <summary>
        /// Generate the body of the <c>decode</c> function for the given <see cref="IDefinition"/>.
        /// </summary>
        /// <param name="definition">The definition to generate code for.</param>
        /// <returns>The generated CPlusPlus <c>decode</c> function body.</returns>
        public string CompileDecode(IDefinition definition)
        {
            return definition.Kind switch
            {
                AggregateKind.Message => CompileDecodeMessage(definition),
                AggregateKind.Struct => CompileDecodeStruct(definition),
                _ => throw new InvalidOperationException($"invalid CompileDecode kind: {definition.Kind} in {definition}"),
            };
        }

        /// <summary>
        /// Generate the body of the <c>decode</c> function for the given <see cref="IDefinition"/>,
        /// given that its "kind" is Message.
        /// </summary>
        /// <param name="definition">The message definition to generate code for.</param>
        /// <returns>The generated CPlusPlus <c>decode</c> function body.</returns>
        private string CompileDecodeMessage(IDefinition definition)
        {
            var builder = new IndentedStringBuilder(4);
            builder.AppendLine("const auto length = reader.readMessageLength();");
            builder.AppendLine("const auto end = reader.pointer() + length;");
            builder.AppendLine("while (true) {");
            builder.Indent(2);
            builder.AppendLine("switch (reader.readByte()) {");
            builder.AppendLine("  case 0:");
            builder.AppendLine("    return;");
            foreach (var field in definition.Fields)
            {
                builder.AppendLine($"  case {field.ConstantValue}:");
                builder.AppendLine($"    {CompileDecodeField(field.Type, $"target.{field.Name}", 0, 2, true)}");
                builder.AppendLine("    break;");
            }
            builder.AppendLine("  default:");
            builder.AppendLine("    reader.seek(end);");
            builder.AppendLine("    return;");
            builder.AppendLine("}");
            builder.Dedent(2);
            builder.AppendLine("}");
            return builder.ToString();
        }

        private string CompileDecodeStruct(IDefinition definition)
        {
            var builder = new IndentedStringBuilder(4);
            int i = 0;
            foreach (var field in definition.Fields)
            {
                builder.AppendLine(CompileDecodeField(field.Type, $"target.{field.Name}", 0, 0, false));
                i++;
            }
            // var args = string.Join(", ", definition.Fields.Select((field, i) => $"field{i}"));
            // builder.AppendLine($"return {definition.Name} {{ {args} }};");
            return builder.ToString();
        }

        private string CompileDecodeField(TypeBase type, string target, int depth = 0, int indentDepth = 0, bool isOptional = false)
        {
            var tab = new string(' ', indentStep);
            var nl = "\n" + new string(' ', indentDepth * indentStep);
            var i = GeneratorUtils.LoopVariable(depth);
            var dot = isOptional ? "->" : ".";
            return type switch
            {
                ArrayType at when at.IsBytes() => $"{target} = reader.readBytes();",
                ArrayType at =>
                    $"{{" + nl +
                    $"{tab}const auto length{depth} = reader.readUint32();" + nl +
                    $"{tab}{target} = {TypeName(at)}();" + nl +
                    $"{tab}{target}{dot}reserve(length{depth});" + nl +
                    $"{tab}for (size_t {i} = 0; {i} < length{depth}; {i}++) {{" + nl +
                    $"{tab}{tab}{TypeName(at.MemberType)} x{depth};" + nl +
                    $"{tab}{tab}{CompileDecodeField(at.MemberType, $"x{depth}", depth + 1, indentDepth + 2, isOptional)}" + nl +
                    $"{tab}{tab}{target}{dot}push_back(x{depth});" + nl +
                    $"{tab}}}" + nl +
                    $"}}",
                MapType mt =>
                    $"{{" + nl +
                    $"{tab}const auto length{depth} = reader.readUint32();" + nl +
                    $"{tab}{target} = {TypeName(mt)}();" + nl +
                    $"{tab}for (size_t {i} = 0; {i} < length{depth}; {i}++) {{" + nl +
                    $"{tab}{tab}{TypeName(mt.KeyType)} k{depth};" + nl +
                    $"{tab}{tab}{CompileDecodeField(mt.KeyType, $"k{depth}", depth + 1, indentDepth + 2, isOptional)}" + nl +
                    $"{tab}{tab}{TypeName(mt.ValueType)}& v{depth} = {target}{dot}operator[](k{depth});" + nl +
                    $"{tab}{tab}{CompileDecodeField(mt.ValueType, $"v{depth}", depth + 1, indentDepth + 2, isOptional)}" + nl +
                    $"{tab}}}" + nl +
                    $"}}",
                ScalarType st => st.BaseType switch
                {
                    BaseType.Bool => $"{target} = reader.readBool();",
                    BaseType.Byte => $"{target} = reader.readByte();",
                    BaseType.UInt16 => $"{target} = reader.readUint16();",
                    BaseType.Int16 => $"{target} = reader.readInt16();",
                    BaseType.UInt32 => $"{target} = reader.readUint32();",
                    BaseType.Int32 => $"{target} = reader.readInt32();",
                    BaseType.UInt64 => $"{target} = reader.readUint64();",
                    BaseType.Int64 => $"{target} = reader.readInt64();",
                    BaseType.Float32 => $"{target} = reader.readFloat32();",
                    BaseType.Float64 => $"{target} = reader.readFloat64();",
                    BaseType.String => $"{target} = reader.readString();",
                    BaseType.Guid => $"{target} = reader.readGuid();",
                    BaseType.Date => $"{target} = reader.readDate();",
                    _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                },
                DefinedType dt when Schema.Definitions[dt.Name].Kind == AggregateKind.Enum =>
                    $"{target} = static_cast<{dt.Name}>(reader.readUint32());",
                DefinedType dt => $"{dt.Name}::decodeInto({target}, reader);",
                _ => throw new InvalidOperationException($"CompileDecodeField: {type}")
            };
        }

        /// <summary>
        /// Generate a CPlusPlus type name for the given <see cref="TypeBase"/>.
        /// </summary>
        /// <param name="type">The field type to generate code for.</param>
        /// <returns>The CPlusPlus type name.</returns>
        private string TypeName(in TypeBase type)
        {
            switch (type)
            {
                case ScalarType st:
                    return st.BaseType switch
                    {
                        BaseType.Bool => "bool",
                        BaseType.Byte => "uint8_t",
                        BaseType.UInt16 => "uint16_t",
                        BaseType.Int16 => "int16_t",
                        BaseType.UInt32 => "uint32_t",
                        BaseType.Int32 => "int32_t",
                        BaseType.UInt64 => "uint64_t",
                        BaseType.Int64 => "int64_t",
                        BaseType.Float32 => "float",
                        BaseType.Float64 => "double",
                        BaseType.String => "std::string",
                        BaseType.Guid => "bebop::Guid",
                        BaseType.Date => "bebop::TickDuration",
                        _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                    };
                // case ArrayType at when at.IsBytes():
                //     return "std::vector<uint8_t>";
                case ArrayType at:
                    return $"std::vector<{TypeName(at.MemberType)}>";
                case MapType mt:
                    return $"std::map<{TypeName(mt.KeyType)}, {TypeName(mt.ValueType)}>";
                case DefinedType dt:
                    var isEnum = Schema.Definitions[dt.Name].Kind == AggregateKind.Enum;
                    return dt.Name;
            }
            throw new InvalidOperationException($"GetTypeName: {type}");
        }

        private string Optional(string type) {
            return "std::optional<" + type + ">";
        }

        /// <summary>
        /// Generate code for a Bebop schema.
        /// </summary>
        /// <returns>The generated code.</returns>
        public override string Compile()
        {
            var builder = new StringBuilder();
            builder.AppendLine("#include <cstddef>");
            builder.AppendLine("#include <cstdint>");
            builder.AppendLine("#include <map>");
            builder.AppendLine("#include <memory>");
            builder.AppendLine("#include <optional>");
            builder.AppendLine("#include <string>");
            builder.AppendLine("#include <vector>");
            builder.AppendLine("#include \"bebop.hpp\"");
            builder.AppendLine("");

            if (!string.IsNullOrWhiteSpace(Schema.Namespace))
            {
                builder.AppendLine($"namespace {Schema.Namespace} {{");
                builder.AppendLine("");
            }

            foreach (var definition in Schema.Definitions.Values)
            {
                if (!string.IsNullOrWhiteSpace(definition.Documentation))
                {
                    builder.Append(FormatDocumentation(definition.Documentation, 0));
                }
                switch (definition.Kind)
                {
                    case AggregateKind.Enum:
                        builder.AppendLine($"enum class {definition.Name} {{");
                        for (var i = 0; i < definition.Fields.Count; i++)
                        {
                            var field = definition.Fields.ElementAt(i);
                            if (!string.IsNullOrWhiteSpace(field.Documentation))
                            {
                                builder.Append(FormatDocumentation(field.Documentation, 2));
                            }
                            if (field.DeprecatedAttribute != null)
                            {
                                builder.AppendLine($"  /// @deprecated {field.DeprecatedAttribute.Value}");
                            }
                            builder.AppendLine($"  {field.Name} = {field.ConstantValue},");
                        }
                        builder.AppendLine("};");
                        builder.AppendLine("");
                        break;
                    default:
                        var isMessage = definition.Kind == AggregateKind.Message;
                        builder.AppendLine($"struct {definition.Name} {{");
                        for (var i = 0; i < definition.Fields.Count; i++)
                        {
                            var field = definition.Fields.ElementAt(i);
                            var type = TypeName(field.Type);
                            if (!string.IsNullOrWhiteSpace(field.Documentation))
                            {
                                builder.Append(FormatDocumentation(field.Documentation, 2));
                            }
                            if (field.DeprecatedAttribute != null)
                            {
                                builder.AppendLine($"  /// @deprecated {field.DeprecatedAttribute.Value}");
                            }
                            builder.AppendLine($"  {(isMessage ? Optional(type) : type)} {field.Name};");
                        }
                        if (isMessage)
                        {
                            // builder.AppendLine($"  {definition.Name}();");
                        }
                        else
                        {
                            // builder.AppendLine($"  {(definition.IsReadOnly ? "const " : "")}{definition.Name}({{");
                            // foreach (var field in definition.Fields)
                            // {
                            //     builder.AppendLine($"    @required this.{field.Name},");
                            // }
                            // builder.AppendLine("  });");
                        }
                        builder.AppendLine("");
                        if (definition.OpcodeAttribute != null)
                        {
                            builder.AppendLine($"  static const uint32_t opcode = {definition.OpcodeAttribute.Value};");
                            builder.AppendLine("");
                        }
                        builder.AppendLine($"  static std::unique_ptr<std::vector<uint8_t>> encode(const {definition.Name}& message) {{");
                        builder.AppendLine("    bebop::BebopWriter writer{};");
                        builder.AppendLine($"    {definition.Name}::encodeInto(message, writer);");
                        builder.AppendLine("    return writer.buffer();");
                        builder.AppendLine("  }");
                        builder.AppendLine("");
                        builder.AppendLine($"  static void encodeInto(const {definition.Name}& message, bebop::BebopWriter& writer) {{");
                        builder.Append(CompileEncode(definition));
                        builder.AppendLine("  }");
                        builder.AppendLine("");
                        builder.AppendLine($"  static {definition.Name} decode(const uint8_t *buffer) {{");
                        builder.AppendLine($"    {definition.Name} result;");
                        builder.AppendLine("    bebop::BebopReader reader{buffer};");
                        builder.AppendLine($"    {definition.Name}::decodeInto(result, reader);");
                        builder.AppendLine($"    return result;");
                        builder.AppendLine("  }");
                        builder.AppendLine("");
                        builder.AppendLine($"  static void decodeInto({definition.Name}& target, bebop::BebopReader& reader) {{");
                        builder.Append(CompileDecode(definition));
                        builder.AppendLine("  }");
                        builder.AppendLine("};");
                        builder.AppendLine("");
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(Schema.Namespace))
            {
                builder.AppendLine($"}} // namespace {Schema.Namespace}");
                builder.AppendLine("");
            }

            return builder.ToString();
        }

        public override void WriteAuxiliaryFiles(string outputPath)
        {
            // There is nothing to do here.
        }
    }
}
