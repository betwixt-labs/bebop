using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
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
            builder.AppendLine($"pos = writer.reserveMessageLength()");
            builder.AppendLine($"start = writer.length");
            foreach (var field in definition.Fields)
            {
                if (field.DeprecatedAttribute != null)
                {
                    continue;
                }
                builder.AppendLine($"if message.{field.Name} is not None:");
                builder.AppendLine($"  writer.writeByte({field.ConstantValue})");
                builder.AppendLine($"  {CompileEncodeField(field.Type, $"message.{field.Name}")}");
            }
            builder.AppendLine("writer.writeByte(0)");
            builder.AppendLine("end = writer.length");
            builder.AppendLine("writer.fillMessageLength(pos, end - start)");
            return builder.ToString();
        }

        private string CompileEncodeStruct(StructDefinition definition)
        {
            var builder = new IndentedStringBuilder(4);
            foreach (var field in definition.Fields)
            {
                builder.AppendLine(CompileEncodeField(field.Type, $"message.{field.Name}"));
                builder.AppendLine("");
            }
            if (definition.Fields.Count == 0) {
                builder.AppendLine("pass");
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
                ArrayType at when at.IsBytes() => $"writer.writeBytes({target})",
                ArrayType at =>
                    $"length{depth} = {target}.length" + nl +
                    $"writer.writeUint32(length{depth})" + nl +
                    $"for {i} in range(length{depth}):" + nl +
                    $"{tab}{CompileEncodeField(at.MemberType, $"{target}[{i}]", depth + 1, indentDepth + 1)}",
                MapType mt =>
                    $"writer.writeUint32({target}.length)" + nl +
                    $"for e{depth} in {target}.entries:" + nl +
                    $"{tab}{CompileEncodeField(mt.KeyType, $"e{depth}.key", depth + 1, indentDepth + 1)}" + nl +
                    $"{tab}{CompileEncodeField(mt.ValueType, $"e{depth}.value", depth + 1, indentDepth + 1)}" + nl,
                ScalarType st => st.BaseType switch
                {
                    BaseType.Bool => $"writer.writeBool({target})",
                    BaseType.Byte => $"writer.writeByte({target})",
                    BaseType.UInt16 => $"writer.writeUint16({target})",
                    BaseType.Int16 => $"writer.writeInt16({target})",
                    BaseType.UInt32 => $"writer.writeUint32({target})",
                    BaseType.Int32 => $"writer.writeInt32({target})",
                    BaseType.UInt64 => $"writer.writeUint64({target})",
                    BaseType.Int64 => $"writer.writeInt64({target})",
                    BaseType.Float32 => $"writer.writeFloat32({target})",
                    BaseType.Float64 => $"writer.writeFloat64({target})",
                    BaseType.String => $"writer.writeString({target})",
                    BaseType.Guid => $"writer.writeGuid({target})",
                    BaseType.Date => $"writer.writeDate({target})",
                    _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                },
                DefinedType dt when Schema.Definitions[dt.Name] is EnumDefinition =>
                    $"writer.writeEnum({target})",
                DefinedType dt => $"{dt.Name}.encodeInto({target}, writer)",
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
            builder.AppendLine($"message = {definition.Name}()");
            builder.AppendLine("length = reader.readMessageLength()");
            builder.AppendLine("end = reader.index + length");
            builder.AppendLine("while True:");
            builder.Indent(2);
            builder.AppendLine("byte = reader.readByte()");
            builder.AppendLine("if byte == 0:");
            builder.AppendLine("  return message");
            foreach (var field in definition.Fields)
            {
                builder.AppendLine($"elif byte == {field.ConstantValue}:");
                builder.AppendLine($"  {CompileDecodeField(field.Type, $"message.{field.Name}")}");
            }
            builder.AppendLine("else:");
            builder.AppendLine("  reader.index = end");
            builder.AppendLine("  return message");
            builder.Dedent(2);
            return builder.ToString();
        }

        private string CompileDecodeStruct(StructDefinition definition)
        {
            var builder = new IndentedStringBuilder(4);
            int i = 0;
            foreach (var field in definition.Fields)
            {
                builder.AppendLine(CompileDecodeField(field.Type, $"field{i}"));
                builder.AppendLine("");
                i++;
            }
            var args = string.Join(", ", definition.Fields.Select((field, i) => $"{field.Name}=field{i}"));
            builder.AppendLine($"return {definition.Name}({args})");
            return builder.ToString();
        }

        private string CompileDecodeField(TypeBase type, string target, int depth = 0)
        {
            var tab = new string(' ', indentStep);
            var nl = "\n" + new string(' ', depth * indentStep);
            var i = GeneratorUtils.LoopVariable(depth);
            return type switch
            {
                ArrayType at when at.IsBytes() => $"{target} = reader.readBytes()",
                ArrayType at =>
                    $"length{depth} = reader.readUint32()" + nl +
                    $"{target} = []" + nl +
                    $"for {i} in range(length{depth}):" + nl +
                    $"{tab}{CompileDecodeField(at.MemberType, $"x{depth}", depth + 1)}" + nl +
                    $"{tab}{target}[{i}] = x{depth}" + nl,
                MapType mt =>
                    $"length{depth} = reader.readUint32()" + nl +
                    $"{target} = {{}}" + nl +
                    $"for {i} in range(length{depth}):" + nl +
                    $"{tab}{CompileDecodeField(mt.KeyType, $"k{depth}", depth + 1)}" + nl +
                    $"{tab}{CompileDecodeField(mt.ValueType, $"v{depth}", depth + 1)}" + nl +
                    $"{tab}{target}[k{depth}] = v{depth}" + nl,
                ScalarType st => st.BaseType switch
                {
                    BaseType.Bool => $"{target} = reader.readBool()",
                    BaseType.Byte => $"{target} = reader.readByte()",
                    BaseType.UInt16 => $"{target} = reader.readUint16()",
                    BaseType.Int16 => $"{target} = reader.readInt16()",
                    BaseType.UInt32 => $"{target} = reader.readUint32()",
                    BaseType.Int32 => $"{target} = reader.readInt32()",
                    BaseType.UInt64 => $"{target} = reader.readUint64()",
                    BaseType.Int64 => $"{target} = reader.readInt64()",
                    BaseType.Float32 => $"{target} = reader.readFloat32()",
                    BaseType.Float64 => $"{target} = reader.readFloat64()",
                    BaseType.String => $"{target} = reader.readString()",
                    BaseType.Guid => $"{target} = reader.readGuid()",
                    BaseType.Date => $"{target} = reader.readDate()",
                    _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                },
                DefinedType dt when Schema.Definitions[dt.Name] is EnumDefinition =>
                    $"{target} = {dt.Name}.fromRawValue(reader.readUint32())",
                DefinedType dt => $"{target} = {dt.Name}.readFrom(reader)",
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

        private static string EscapeStringLiteral(string value)
        {
            // Dart accepts \u0000 style escape sequences, so we can escape the string JSON-style.
            var options = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            return JsonSerializer.Serialize(value, options);
        }

        private string EmitLiteral(Literal literal) {
            return literal switch
            {
                BoolLiteral bl => bl.Value ? "true" : "false",
                IntegerLiteral il => il.Value,
                FloatLiteral fl when fl.Value == "inf" => $"math.inf",
                FloatLiteral fl when fl.Value == "-inf" => $"-math.inf",
                FloatLiteral fl when fl.Value == "nan" => $"math.nan",
                FloatLiteral fl => fl.Value,
                StringLiteral sl => EscapeStringLiteral(sl.Value),
                GuidLiteral gl => EscapeStringLiteral(gl.Value.ToString("D")),
                _ => throw new ArgumentOutOfRangeException(literal.ToString()),
            };
        }

        /// <summary>
        /// Generate code for a Bebop schema.
        /// </summary>
        /// <returns>The generated code.</returns>
        public override string Compile(Version? languageVersion, TempoServices services = TempoServices.Both, bool writeGeneratedNotice = true, bool emitBinarySchema = false)
        {
            var builder = new IndentedStringBuilder();
            builder.AppendLine("from enum import Enum");
            builder.AppendLine("from python_bebop import BebopWriter, BebopReader");
            builder.AppendLine("from uuid import UUID");
            builder.AppendLine("import math");
            builder.AppendLine("from datetime import datetime");
            builder.AppendLine("");

            foreach (var definition in Schema.Definitions.Values)
            {
                if (!string.IsNullOrWhiteSpace(definition.Documentation))
                {
                    builder.Append(FormatDocumentation(definition.Documentation));
                    builder.AppendLine("");
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
                                builder.AppendLine("");
                            }
                            if (field.DeprecatedAttribute != null)
                            {
                                
                                builder.AppendLine($"\"\"\" @deprecated {field.DeprecatedAttribute.Value}  \"\"\"");
                            }
                            builder.AppendLine($"{field.Name.ToUpper()} = {field.ConstantValue}");
                        }
                        builder.AppendLine("");
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
                                builder.AppendLine("");
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
                        builder.AppendLine("");
                        if (!(fd is MessageDefinition)) {
                            List<string> fields = new List<string>();
                            foreach (var field in fd.Fields)
                            {
                                fields.Add($" {field.Name}: {TypeName(field.Type)}");
                            }
                            if (fields.Count != 0) {
                                builder.Append("def __init__(self, ");
                                builder.Append(string.Join(",", fields));
                                builder.AppendLine("):");
                                builder.Indent(indentStep);
                                foreach (var field in fd.Fields) {
                                    builder.AppendLine($"self.{field.Name} = {field.Name}");
                                }
                                builder.Dedent(indentStep);
                            }
                        }
                        builder.AppendLine("");

                        builder.AppendLine("@staticmethod");
                        builder.AppendLine($"def encode(message: \"{fd.Name}\"):");
                        builder.Indent(indentStep);
                        builder.AppendLine("writer = BebopWriter()");
                        builder.AppendLine($"{fd.Name}.encode_into(message, writer)");
                        builder.AppendLine("return writer.to_list()");
                        builder.Dedent(indentStep);
                        builder.AppendLine("");
                        builder.AppendLine("");

                        builder.AppendLine("@staticmethod");
                        builder.AppendLine($"def encode_into(message: \"{fd.Name}\", writer: BebopWriter):");
                        builder.Append(CompileEncode(fd));
                        builder.AppendLine("");
                        builder.AppendLine("");

                        builder.AppendLine("@classmethod");
                        builder.AppendLine($"def _read_from(cls, reader: BebopReader):");
                        builder.Append(CompileDecode(fd));
                        builder.AppendLine("");
                        builder.AppendLine("");

                        builder.AppendLine("@staticmethod");
                        builder.AppendLine($"def decode(buffer) -> \"{fd.Name}\":");
                        builder.Indent(indentStep);
                        builder.AppendLine($"return {fd.Name}._read_from(BebopReader(buffer))");
                        builder.Dedent(indentStep);
                        builder.AppendLine("");
                        builder.AppendLine("");

                        builder.Dedent(indentStep);
                        break;
                    case ConstDefinition cd:
                        builder.AppendLine($"{cd.Name} = {EmitLiteral(cd.Value)}");
                        builder.AppendLine("");
                        break;
                    case ServiceDefinition:
                        break;
                    default:
                        throw new InvalidOperationException($"unsupported definition {definition}");
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
