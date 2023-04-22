using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Meta;
using Core.Meta.Extensions;

namespace Core.Generators.Python
{
    public class PythonGenerator : BaseGenerator
    {
        const int indentStep = 4;

        public PythonGenerator(BebopSchema schema) : base(schema) { }

        private string FormatDocumentation(string documentation){
            var builder = new StringBuilder();
            foreach (var line in documentation.GetLines())
            {
                builder.AppendLine($"# {line}");
            }
            return builder.ToString();
        }

        private static string FormatDeprecationDoc(string deprecationReason, int spaces)
        {
            var builder = new IndentedStringBuilder();
            builder.Indent(spaces);
            builder.AppendLine("\"\"\"");
            builder.Indent(1);
            builder.AppendLine($"@deprecated {deprecationReason}");
            builder.AppendLine("\"\"\"");
            return builder.ToString();
        }

        /// <summary>
        /// Generate the body of the <c>encode</c> function for the given <see cref="FieldsDefinition"/>.
        /// </summary>
        /// <param name="definition">The definition to generate code for.</param>
        /// <returns>The generated Python <c>encode</c> function body.</returns>
        public string CompileEncode(FieldsDefinition definition)
        {
            return definition switch
            {
                MessageDefinition d => CompileEncodeMessage(d),
                StructDefinition d => CompileEncodeStruct(d),
                _ => throw new InvalidOperationException($"invalid CompileEncode value: {definition}"),
            };
        }

        private string CompileEncodeMessage(MessageDefinition definition)
        {
            var builder = new IndentedStringBuilder(4);
            builder.AppendLine($"final pos = view.reserveMessageLength();");
            builder.AppendLine($"final start = view.length;");
            foreach (var field in definition.Fields)
            {
                if (field.DeprecatedAttribute != null)
                {
                    continue;
                }
                builder.AppendLine($"if (message.{field.Name} != null) {{");
                builder.AppendLine($"  view.writeByte({field.ConstantValue});");
                builder.AppendLine($"  {CompileEncodeField(field.Type, $"message.{field.Name}")}");
                builder.AppendLine($"}}");
            }
            builder.AppendLine("view.writeByte(0);");
            builder.AppendLine("final end = view.length;");
            builder.AppendLine("view.fillMessageLength(pos, end - start);");
            return builder.ToString();
        }

        private string CompileEncodeStruct(StructDefinition definition)
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
                ArrayType at when at.IsBytes() => $"view.writeBytes({target});",
                ArrayType at =>
                    $"{{" + nl +
                    $"{tab}final length{depth} = {target}.length;" + nl +
                    $"{tab}view.writeUint32(length{depth});" + nl +
                    $"{tab}for (var {i} = 0; {i} < length{depth}; {i}++) {{" + nl +
                    $"{tab}{tab}{CompileEncodeField(at.MemberType, $"{target}[{i}]", depth + 1, indentDepth + 2)}" + nl +
                    $"{tab}}}" + nl +
                    $"}}",
                MapType mt =>
                    $"view.writeUint32({target}.length);" + nl +
                    $"for (final e{depth} in {target}.entries) {{" + nl +
                    $"{tab}{CompileEncodeField(mt.KeyType, $"e{depth}.key", depth + 1, indentDepth + 1)}" + nl +
                    $"{tab}{CompileEncodeField(mt.ValueType, $"e{depth}.value", depth + 1, indentDepth + 1)}" + nl +
                    $"}}",
                ScalarType st => st.BaseType switch
                {
                    BaseType.Bool => $"view.writeBool({target});",
                    BaseType.Byte => $"view.writeByte({target});",
                    BaseType.UInt16 => $"view.writeUint16({target});",
                    BaseType.Int16 => $"view.writeInt16({target});",
                    BaseType.UInt32 => $"view.writeUint32({target});",
                    BaseType.Int32 => $"view.writeInt32({target});",
                    BaseType.UInt64 => $"view.writeUint64({target});",
                    BaseType.Int64 => $"view.writeInt64({target});",
                    BaseType.Float32 => $"view.writeFloat32({target});",
                    BaseType.Float64 => $"view.writeFloat64({target});",
                    BaseType.String => $"view.writeString({target});",
                    BaseType.Guid => $"view.writeGuid({target});",
                    BaseType.Date => $"view.writeDate({target});",
                    _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                },
                DefinedType dt when Schema.Definitions[dt.Name] is EnumDefinition =>
                    $"view.writeEnum({target});",
                DefinedType dt => $"{dt.Name}.encodeInto({target}, view);",
                _ => throw new InvalidOperationException($"CompileEncodeField: {type}")
            };
        }

        /// <summary>
        /// Generate the body of the <c>decode</c> function for the given <see cref="FieldsDefinition"/>.
        /// </summary>
        /// <param name="definition">The definition to generate code for.</param>
        /// <returns>The generated Dart <c>decode</c> function body.</returns>
        public string CompileDecode(FieldsDefinition definition)
        {
            return definition switch
            {
                MessageDefinition d => CompileDecodeMessage(d),
                StructDefinition d => CompileDecodeStruct(d),
                _ => throw new InvalidOperationException($"invalid CompileDecodevalue: {definition}"),
            };
        }

        /// <summary>
        /// Generate the body of the <c>decode</c> function for the given <see cref="MessageDefinition"/>.
        /// </summary>
        /// <param name="definition">The message definition to generate code for.</param>
        /// <returns>The generated Dart <c>decode</c> function body.</returns>
        private string CompileDecodeMessage(MessageDefinition definition)
        {
            var builder = new IndentedStringBuilder(4);
            builder.AppendLine($"var message = {definition.Name}();");
            builder.AppendLine("final length = view.readMessageLength();");
            builder.AppendLine("final end = view.index + length;");
            builder.AppendLine("while (true) {");
            builder.Indent(2);
            builder.AppendLine("switch (view.readByte()) {");
            builder.AppendLine("  case 0:");
            builder.AppendLine("    return message;");
            foreach (var field in definition.Fields)
            {
                builder.AppendLine($"  case {field.ConstantValue}:");
                builder.AppendLine($"    {CompileDecodeField(field.Type, $"message.{field.Name}")}");
                builder.AppendLine("    break;");
            }
            builder.AppendLine("  default:");
            builder.AppendLine("    view.index = end;");
            builder.AppendLine("    return message;");
            builder.AppendLine("}");
            builder.Dedent(2);
            builder.AppendLine("}");
            return builder.ToString();
        }

        private string CompileDecodeStruct(StructDefinition definition)
        {
            var builder = new IndentedStringBuilder(4);
            int i = 0;
            foreach (var field in definition.Fields)
            {
                builder.AppendLine($"{TypeName(field.Type)} field{i};");
                builder.AppendLine(CompileDecodeField(field.Type, $"field{i}"));
                i++;
            }
            var args = string.Join(", ", definition.Fields.Select((field, i) => $"{field.Name}: field{i}"));
            builder.AppendLine($"return {definition.Name}({args});");
            return builder.ToString();
        }

        private string CompileDecodeField(TypeBase type, string target, int depth = 0)
        {
            var tab = new string(' ', indentStep);
            var nl = "\n" + new string(' ', depth * 2 * indentStep);
            var i = GeneratorUtils.LoopVariable(depth);
            return type switch
            {
                ArrayType at when at.IsBytes() => $"{target} = view.readBytes();",
                ArrayType at =>
                    $"{{" + nl +
                    $"{tab}var length{depth} = view.readUint32();" + nl +
                    $"{tab}{target} = {TypeName(at)}(length{depth});" + nl +
                    $"{tab}for (var {i} = 0; {i} < length{depth}; {i}++) {{" + nl +
                    $"{tab}{tab}{TypeName(at.MemberType)} x{depth};" + nl +
                    $"{tab}{tab}{CompileDecodeField(at.MemberType, $"x{depth}", depth + 1)}" + nl +
                    $"{tab}{tab}{target}[{i}] = x{depth};" + nl +
                    $"{tab}}}" + nl +
                    $"}}",
                MapType mt =>
                    $"{{" + nl +
                    $"{tab}var length{depth} = view.readUint32();" + nl +
                    $"{tab}{target} = {TypeName(mt)}();" + nl +
                    $"{tab}for (var {i} = 0; {i} < length{depth}; {i}++) {{" + nl +
                    $"{tab}{tab}{TypeName(mt.KeyType)} k{depth};" + nl +
                    $"{tab}{tab}{TypeName(mt.ValueType)} v{depth};" + nl +
                    $"{tab}{tab}{CompileDecodeField(mt.KeyType, $"k{depth}", depth + 1)}" + nl +
                    $"{tab}{tab}{CompileDecodeField(mt.ValueType, $"v{depth}", depth + 1)}" + nl +
                    $"{tab}{tab}{target}[k{depth}] = v{depth};" + nl +
                    $"{tab}}}" + nl +
                    $"}}",
                ScalarType st => st.BaseType switch
                {
                    BaseType.Bool => $"{target} = view.readBool();",
                    BaseType.Byte => $"{target} = view.readByte();",
                    BaseType.UInt16 => $"{target} = view.readUint16();",
                    BaseType.Int16 => $"{target} = view.readInt16();",
                    BaseType.UInt32 => $"{target} = view.readUint32();",
                    BaseType.Int32 => $"{target} = view.readInt32();",
                    BaseType.UInt64 => $"{target} = view.readUint64();",
                    BaseType.Int64 => $"{target} = view.readInt64();",
                    BaseType.Float32 => $"{target} = view.readFloat32();",
                    BaseType.Float64 => $"{target} = view.readFloat64();",
                    BaseType.String => $"{target} = view.readString();",
                    BaseType.Guid => $"{target} = view.readGuid();",
                    BaseType.Date => $"{target} = view.readDate();",
                    _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                },
                DefinedType dt when Schema.Definitions[dt.Name] is EnumDefinition =>
                    $"{target} = {dt.Name}.fromRawValue(view.readUint32());",
                DefinedType dt => $"{target} = {dt.Name}.readFrom(view);",
                _ => throw new InvalidOperationException($"CompileDecodeField: {type}")
            };
        }

        /// <summary>
        /// Generate a Python type name for the given <see cref="TypeBase"/>.
        /// </summary>
        /// <param name="type">The field type to generate code for.</param>
        /// <returns>The Python type name.</returns>
        private string TypeName(in TypeBase type)
        {
            switch (type)
            {
                case ScalarType st:
                    return st.BaseType switch
                    {
                        BaseType.Bool => "bool",
                        BaseType.Byte or BaseType.UInt16 or BaseType.Int16 or BaseType.UInt32 or BaseType.Int32 or BaseType.UInt64 or BaseType.Int64 => "int",
                        BaseType.Float32 or BaseType.Float64 => "float",
                        BaseType.String => "str",
                        BaseType.Guid => "UUID",
                        BaseType.Date => "datetime",
                        _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                    };
                case ArrayType at when at.IsBytes():
                    return "bytearray";
                case ArrayType at:
                    return $"list[{TypeName(at.MemberType)}]";
                case MapType mt:
                    return $"dict[{TypeName(mt.KeyType)}, {TypeName(mt.ValueType)}]";
                case DefinedType dt:
                    var isEnum = Schema.Definitions[dt.Name] is EnumDefinition;
                    return dt.Name;
            }
            throw new InvalidOperationException($"GetTypeName: {type}");
        }

        /// <summary>
        /// Generate code for a Bebop schema.
        /// </summary>
        /// <returns>The generated code.</returns>
        public override string Compile(Version? languageVersion, bool writeGeneratedNotice = true)
        {
            var builder = new IndentedStringBuilder();
            builder.AppendLine("from enum import Enum");
            builder.AppendLine("import python_bebop");
            builder.AppendLine("from uuid import UUID");
            builder.AppendLine("from datetime import datetime");
            builder.AppendLine("");

            foreach (var definition in Schema.Definitions.Values)
            {
                if (!string.IsNullOrWhiteSpace(definition.Documentation))
                {
                    builder.Append(FormatDocumentation(definition.Documentation));
                }
                switch (definition)
                {
                    case EnumDefinition ed:
                        builder.AppendLine($"class {ed.Name}(Enum):");
                        builder.Indent(indentStep);
                        for (var i = 0; i < ed.Members.Count; i++)
                        {
                            var field = ed.Members.ElementAt(i);
                            if (!string.IsNullOrWhiteSpace(field.Documentation))
                            {
                                builder.Append(FormatDocumentation(field.Documentation));
                            }
                            if (field.DeprecatedAttribute != null)
                            {
                                builder.AppendLine($"# @deprecated {field.DeprecatedAttribute.Value}");
                            }
                            builder.AppendLine($"  {field.Name.ToUpper()} = {field.ConstantValue}");
                        }
                        builder.Dedent(indentStep);
                        break;
                    case FieldsDefinition fd:
                        builder.AppendLine($"class {fd.Name}:");
                        builder.Indent(indentStep);
                        for (var i = 0; i < fd.Fields.Count; i++) {
                            var field = fd.Fields.ElementAt(i);
                            var type = TypeName(field.Type);
                            if (!string.IsNullOrWhiteSpace(field.Documentation))
                            {
                                builder.Append(FormatDocumentation(field.Documentation));
                            }
                            if (field.DeprecatedAttribute != null)
                            {
                                builder.AppendLine($"# @deprecated {field.DeprecatedAttribute.Value}");
                            }
                            builder.AppendLine($"{field.Name}: {type}");
                        }
                        if (fd.OpcodeAttribute != null) {
                            builder.AppendLine($"opcode = {fd.OpcodeAttribute.Value}");
                            builder.AppendLine("");
                        }
                        if (!(fd is MessageDefinition)) {
                            builder.AppendLine("def __init__(self, ");
                            List<string> fields = new List<string>();
                            foreach (var field in fd.Fields)
                            {
                                fields.Add($"{field.Name}: {field.Type}");
                            }
                            builder.Append(String.Join(",", fields));
                            builder.AppendLine("):");
                            builder.Indent(indentStep);
                            foreach (var field in fd.Fields) {
                                builder.AppendLine($"self.{field.Name} = {field.Name}");
                            }
                            builder.Dedent(indentStep);
                        }
                        builder.AppendLine("");

                        builder.AppendLine("@staticmethod");
                        builder.AppendLine($"encode(message: {fd.Name}):");
                        builder.Indent(indentStep);
                        builder.AppendLine("writer = BebopWriter()");
                        builder.AppendLine($"{fd.Name}.encode_into(message, writer)");
                        builder.AppendLine("return writer.to_list()");
                        builder.Dedent(indentStep);
                        builder.AppendLine("");

                        builder.AppendLine("@staticmethod");
                        builder.AppendLine($"encode_into(message: {fd.Name}, writer: BebopWriter):");
                        builder.Indent(indentStep);
                        builder.Append(CompileEncode(fd));
                        builder.Dedent(indentStep);
                        builder.AppendLine("");

                        builder.AppendLine("@classmethod");
                        builder.AppendLine($"_read_from(cls, reader: BebopReader):");
                        builder.Indent(indentStep);
                        builder.Append(CompileDecode(fd));
                        builder.Dedent(indentStep);
                        builder.AppendLine("");

                        builder.AppendLine("@staticmethod");
                        builder.AppendLine($"decode(buffer) -> {fd.Name}:");
                        builder.Indent(indentStep);
                        builder.AppendLine($"return {fd.Name}._read_from(BebopReader(buffer))");
                        builder.Dedent(indentStep);
                        builder.AppendLine("");

                        builder.Dedent(indentStep);
                        break;
                }
            }

            return builder.ToString();
        }

        public override void WriteAuxiliaryFiles(string outputPath)
        {
            // There is nothing to do here.
        }
    }
}
