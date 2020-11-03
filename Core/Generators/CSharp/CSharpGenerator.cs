using System;
using System.Linq;
using Core.Meta;
using Core.Meta.Extensions;
using Core.Meta.Interfaces;

namespace Core.Generators.CSharp
{
    
    public class CSharpGenerator : Generator
    {
        const int indentStep = 2;
        private static readonly string GeneratedAttribute = $"[System.CodeDom.Compiler.GeneratedCode(\"{ReservedWords.CompilerName}\", \"{ReservedWords.CompilerVersion}\")]";
        private const string HotPath = "[System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]";
        public CSharpGenerator(ISchema schema) : base(schema) { }

        private string FormatDocumentation(string documentation, int spaces)
        {
            var builder = new IndentedStringBuilder(spaces);
            builder.AppendLine("/// <summary>");
            foreach (var line in documentation.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                builder.AppendLine($"/// {line}");
            }
            builder.AppendLine("/// </summary>");
            return builder.ToString();
        }

        public override string Compile()
        {
            var builder = new IndentedStringBuilder();
            builder.AppendLine("using Bebop;");

            if (!string.IsNullOrWhiteSpace(Schema.Namespace))
            {
                builder.AppendLine($"namespace {Schema.Namespace.ToPascalCase()} {{");
                builder.Indent(indentStep);
            }
            foreach (var definition in Schema.Definitions.Values)
            {
                var definitionName = definition.Name.ToPascalCase();
                if (!string.IsNullOrWhiteSpace(definition.Documentation))
                {
                    builder.AppendLine(FormatDocumentation(definition.Documentation, 2));
                }
                builder.AppendLine(GeneratedAttribute);
                if (definition.IsEnum())
                {
                    builder.AppendLine($"public enum {definition.Name} : uint {{");
                    builder.Indent(indentStep);
                    for (var i = 0; i < definition.Fields.Count; i++)
                    {
                        var field = definition.Fields.ElementAt(i);
                        if (!string.IsNullOrWhiteSpace(field.Documentation))
                        {
                            builder.AppendLine(FormatDocumentation(field.Documentation, 6));
                        }
                        builder.AppendLine($"{field.Name} = {field.ConstantValue}{(i + 1 < definition.Fields.Count ? "," : "")}");
                    }
                    builder.Dedent(indentStep);
                    builder.AppendLine("}");
                }
                else if (definition.IsMessage() || definition.IsStruct())
                {
                    var baseName = "Base" + definitionName;
                    builder.AppendLine($"public abstract class {baseName} {{");
                    builder.Indent(indentStep);
                    if (definition.OpcodeAttribute != null)
                    {
                        builder.AppendLine($"public const int Opcode = {definition.OpcodeAttribute.Value};");
                    }
                    if (definition.IsMessage())
                    {
                        builder.AppendLine("#nullable enable");
                    }
                    for (var i = 0; i < definition.Fields.Count; i++)
                    {
                        var field = definition.Fields.ElementAt(i);

                        if (!string.IsNullOrWhiteSpace(field.Documentation))
                        {
                            builder.AppendLine(FormatDocumentation(field.Documentation, 4));
                        }
                        if (field.DeprecatedAttribute != null &&
                            !string.IsNullOrWhiteSpace(field.DeprecatedAttribute.Value))
                        {
                            builder.AppendLine($"[System.Obsolete(\"{field.DeprecatedAttribute.Value}\")]");
                        }
                        var type = TypeName(field.Type);
                        var opt = definition.Kind == AggregateKind.Message ? "?" : "";
                        var setOrInit = definition.IsReadOnly ? "init" : "set";
                        builder.AppendLine($"public {type}{opt} {field.Name.ToPascalCase()} {{ get; {setOrInit}; }}");
                    }
                    if (definition.IsMessage())
                    {
                        builder.AppendLine("#nullable disable");
                    }
                    builder.Dedent(indentStep);
                    builder.AppendLine("}");
                    builder.AppendLine("");
                    builder.AppendLine("/// <inheritdoc />");
                    builder.AppendLine(GeneratedAttribute);
                    builder.AppendLine($"public sealed class {definitionName} : {baseName} {{");
                    builder.Indent(indentStep);
                    builder.AppendLine(CompileEncodeHelper(definition));
                    builder.AppendLine(HotPath);
                    builder.AppendLine(GeneratedAttribute);
                    builder.AppendLine($"public static void EncodeInto({baseName} message, ref BebopView view) {{");
                    builder.Indent(indentStep);
                    builder.AppendLine(CompileEncode(definition));
                    builder.Dedent(indentStep);
                    builder.AppendLine("}");
                    builder.AppendLine("");
                    builder.AppendLine(HotPath);
                    builder.AppendLine(GeneratedAttribute);
                    builder.AppendLine($"public static {baseName} DecodeFrom(ref BebopView view) {{");
                    builder.Indent(indentStep);
                    builder.AppendLine(CompileDecode(definition));
                    builder.Dedent(indentStep);
                    builder.AppendLine("}");
                    builder.Dedent(indentStep);
                    builder.AppendLine("}");
                }
            }

            if (!string.IsNullOrWhiteSpace(Schema.Namespace))
            {
                builder.Dedent(indentStep);
                builder.AppendLine("}");
            }
            return builder.ToString();
        }

        public override void WriteAuxiliaryFiles(string outputPath)
        {

        }

        /// <summary>
        ///     Generate a C# type name for the given <see cref="TypeBase"/>.
        /// </summary>
        /// <param name="type">The field type to generate code for.</param>
        /// <param name="arraySizeVar">A variable name that will be formatted into the array initializer</param>
        /// <returns>The C# type name.</returns>
        private string TypeName(in TypeBase type, string arraySizeVar = "")
        {
            switch (type)
            {
                case ScalarType st:
                    return st.BaseType switch
                    {
                        BaseType.Bool => "bool",
                        BaseType.Byte => "byte",
                        BaseType.UInt32 => "uint",
                        BaseType.Int32 => "int",
                        BaseType.Float32 => "float",
                        BaseType.Float64 => "double",
                        BaseType.String => "string",
                        BaseType.Guid => "System.Guid",
                        BaseType.UInt16 => "ushort",
                        BaseType.Int16 => "short",
                        BaseType.UInt64 => "ulong",
                        BaseType.Int64 => "long",
                        BaseType.Date => "System.DateTime",
                        _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                    };
                case ArrayType at:
                    return $"{(at.MemberType is ArrayType ? ($"{TypeName(at.MemberType, arraySizeVar)}[]") : $"{TypeName(at.MemberType)}[{arraySizeVar}]")}";
                case MapType mt:
                    return $"System.Collections.Generic.Dictionary<{TypeName(mt.KeyType)}, {TypeName(mt.ValueType)}>";
                case DefinedType dt:
                    var isEnum = Schema.Definitions[dt.Name].Kind == AggregateKind.Enum;
                    return $"{(isEnum ? string.Empty : "I")}{dt.Name}";
            }
            throw new InvalidOperationException($"GetTypeName: {type}");
        }

        /// <summary>
        ///     Generate the body of the <c>DecodeFrom</c> function for the given <see cref="IDefinition"/>.
        /// </summary>
        /// <param name="definition">The definition to generate code for.</param>
        /// <returns>The generated C# <c>DecodeFrom</c> function body.</returns>
        public string CompileDecode(IDefinition definition)
        {
            return definition.Kind switch
            {
                AggregateKind.Message => CompileDecodeMessage(definition),
                AggregateKind.Struct => CompileDecodeStruct(definition),
                _ => throw new InvalidOperationException(
                    $"invalid CompileDecode kind: {definition.Kind} in {definition}")
            };
        }

        private string CompileDecodeStruct(IDefinition definition)
        {
            var builder = new IndentedStringBuilder();
            int i = 0;
            foreach (var field in definition.Fields)
            {
                builder.AppendLine($"{TypeName(field.Type)} field{i};");
                builder.AppendLine($"{CompileDecodeField(field.Type, $"field{i}")}");
                i++;
            }

            builder.AppendLine($"return new {definition.Name.ToPascalCase()} {{");
            builder.Indent(indentStep);
            i = 0;
            foreach (var field in definition.Fields)
            {
                builder.AppendLine($"{field.Name.ToPascalCase()} = field{i++},");
            }
            builder.Dedent(indentStep);
            builder.AppendLine("};");
            return builder.ToString();
        }

        /// <summary>
        ///     Generate the body of the <c>DecodeFrom</c> function for the given <see cref="IDefinition"/>,
        ///     given that its "kind" is Message.
        /// </summary>
        /// <param name="definition">The message definition to generate code for.</param>
        /// <returns>The generated C# <c>DecodeFrom</c> function body.</returns>
        private string CompileDecodeMessage(IDefinition definition)
        {
            var builder = new IndentedStringBuilder();
            builder.AppendLine($"var message = new {definition.Name.ToPascalCase()}();");
            builder.AppendLine("var length = view.ReadMessageLength();");
            builder.AppendLine("var end = unchecked((int) (view.Position + length));");
            builder.AppendLine("while (true) {");
            builder.Indent(indentStep);
            builder.AppendLine("switch (view.ReadByte()) {");
            builder.Indent(indentStep);

            // 0 case: end of message
            builder.AppendLine("case 0:");
            builder.Indent(indentStep);
            builder.AppendLine("return message;");
            builder.Dedent(indentStep);

            // cases for fields
            foreach (var field in definition.Fields)
            {
                builder.AppendLine($"case {field.ConstantValue}:");
                builder.Indent(indentStep);
                builder.AppendLine($"{CompileDecodeField(field.Type, $"message.{field.Name.ToPascalCase()}")}");
                builder.AppendLine("break;");
                builder.Dedent(indentStep);
            }

            // default case: unknown, skip to end of message
            builder.AppendLine("default:");
            builder.Indent(indentStep);
            builder.AppendLine("view.Position = end;");
            builder.AppendLine("return message;");
            builder.Dedent(indentStep);

            // end switch:
            builder.Dedent(indentStep);
            builder.AppendLine("}");

            // end while:
            builder.Dedent(indentStep);
            builder.AppendLine("}");
            return builder.ToString();
        }


        private string CompileDecodeField(TypeBase type, string target, int depth = 0)
        {
            var tab = new string(' ', indentStep);
            var nl = "\n" + new string(' ', depth * 2 * indentStep);
            var i = GeneratorUtils.LoopVariable(depth);
            return type switch
            {
                ArrayType at when at.IsBytes() => $"{target} = view.ReadBytes();",
                ArrayType at =>
                    $"{{" + nl +
                    $"{tab}var length{depth} = unchecked((int)view.ReadUInt32());" + nl +
                    $"{tab}{target} = new {TypeName(at, $"length{depth}")};" + nl +
                    $"{tab}for (var {i} = 0; {i} < length{depth}; {i}++) {{" + nl +
                    $"{tab}{tab}{TypeName(at.MemberType)} x{depth};" + nl +
                    $"{tab}{tab}{CompileDecodeField(at.MemberType, $"x{depth}", depth + 1)}" + nl +
                    $"{tab}{tab}{target}[{i}] = x{depth};" + nl +
                    $"{tab}}}" + nl +
                    $"}}",
                MapType mt =>
                    $"{{" + nl +
                    $"{tab}var length{depth} = unchecked((int)view.ReadUInt32());" + nl +
                    $"{tab}{target} = new {TypeName(mt)}(length{depth});" + nl +
                    $"{tab}for (var {i} = 0; {i} < length{depth}; {i}++) {{" + nl +
                    $"{tab}{tab}{TypeName(mt.KeyType)} k{depth};" + nl +
                    $"{tab}{tab}{TypeName(mt.ValueType)} v{depth};" + nl +
                    $"{tab}{tab}{CompileDecodeField(mt.KeyType, $"k{depth}", depth + 1)}" + nl +
                    $"{tab}{tab}{CompileDecodeField(mt.ValueType, $"v{depth}", depth + 1)}" + nl +
                    $"{tab}{tab}{target}.Add(k{depth}, v{depth});" + nl +
                    $"{tab}}}" + nl +
                    $"}}",
                ScalarType st => st.BaseType switch
                {
                    BaseType.Bool => $"{target} = view.ReadByte() != 0;",
                    BaseType.Byte => $"{target} = view.ReadByte();",
                    BaseType.UInt32 => $"{target} = view.ReadUInt32();",
                    BaseType.Int32 => $"{target} = view.ReadInt32();",
                    BaseType.Float32 => $"{target} = view.ReadFloat32();",
                    BaseType.String => $"{target} = view.ReadString();",
                    BaseType.Guid => $"{target} = view.ReadGuid();",
                    BaseType.UInt16 => $"{target} = view.ReadUInt16();",
                    BaseType.Int16 => $"{target} = view.ReadInt16();",
                    BaseType.UInt64 => $"{target} = view.ReadUInt64();",
                    BaseType.Int64 => $"{target} = view.ReadInt64();",
                    BaseType.Float64 => $"{target} = view.ReadFloat64();",
                    BaseType.Date => $"{target} = view.ReadDate();",
                    _ => throw new ArgumentOutOfRangeException()
                },
                DefinedType dt when Schema.Definitions[dt.Name].Kind == AggregateKind.Enum =>
                    $"{target} = view.ReadUint() as {dt.Name};",
                DefinedType dt =>
                    $"{target} = {(string.IsNullOrWhiteSpace(Schema.Namespace) ? string.Empty : $"{Schema.Namespace.ToPascalCase()}.")}{dt.Name.ToPascalCase()}.DecodeFrom(ref view);",
                _ => throw new InvalidOperationException($"CompileDecodeField: {type}")
            };
        }

        /// <summary>
        ///     Generates the body of a helper method to decode the given <see cref="IDefinition"/>
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        public string CompileDecodeHelper(IDefinition definition)
        {
            var builder = new IndentedStringBuilder();
            builder.AppendLine($"public static I{definition.Name.ToPascalCase()} Decode(byte[] message) {{");
            builder.Indent(indentStep);
            builder.AppendLine("var view = new BebopView(message);");
            builder.AppendLine("return DecodeFrom(ref view);");
            builder.Dedent(indentStep);
            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        ///     Generates the body of a helper method to encode the given <see cref="IDefinition"/>
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        public string CompileEncodeHelper(IDefinition definition)
        {
            var builder = new IndentedStringBuilder();
            builder.AppendLine(GeneratedAttribute);
            builder.AppendLine(HotPath);
            builder.AppendLine($"public static byte[] Encode(I{definition.Name.ToPascalCase()} message) {{");
            builder.Indent(indentStep);
            builder.AppendLine("var view = new BebopView();");
            builder.AppendLine("EncodeInto(message, ref view);");
            builder.AppendLine("return view.ToArray();");
            builder.Dedent(indentStep);
            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        ///     Generate the body of the <c>EncodeTo</c> function for the given <see cref="IDefinition"/>.
        /// </summary>
        /// <param name="definition">The definition to generate code for.</param>
        /// <returns>The generated C# <c>EncodeTo</c> function body.</returns>
        public string CompileEncode(IDefinition definition)
        {
            return definition.Kind switch
            {
                AggregateKind.Message => CompileEncodeMessage(definition),
                AggregateKind.Struct => CompileEncodeStruct(definition),
                _ => throw new InvalidOperationException(
                    $"invalid CompileEncode kind: {definition.Kind} in {definition}")
            };
        }

        private string CompileEncodeMessage(IDefinition definition)
        {
            var builder = new IndentedStringBuilder();
            builder.AppendLine($"var pos = view.ReserveMessageLength();");
            builder.AppendLine($"var start = view.Length;");
            foreach (var field in definition.Fields)
            {
                if (field.DeprecatedAttribute != null)
                {
                    continue;
                }
                builder.AppendLine("");
                builder.AppendLine($"if (message.{field.Name.ToPascalCase()}.HasValue) {{");
                builder.Indent(indentStep);
                builder.AppendLine($"view.WriteByte({field.ConstantValue});");
                builder.AppendLine($"{CompileEncodeField(field.Type, $"message.{field.Name.ToPascalCase()}.Value")}");
                builder.Dedent(indentStep);
                builder.AppendLine("}");
            }
            builder.AppendLine("view.WriteByte(0);");
            builder.AppendLine("var end = view.Length;");
            builder.AppendLine("view.FillMessageLength(pos, unchecked((uint) unchecked(end - start)));");
            return builder.ToString();
        }

        private string CompileEncodeStruct(IDefinition definition)
        {
            var builder = new IndentedStringBuilder();
            foreach (var field in definition.Fields)
            {
                builder.AppendLine($"{CompileEncodeField(field.Type, $"message.{field.Name.ToPascalCase()}")}");
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
                ArrayType at when at.IsBytes() => $"view.WriteBytes({target});",
                ArrayType at when at.IsFloat32s() => $"view.WriteFloat32s({target});",
                ArrayType at when at.IsFloat64s() => $"view.WriteFloat64s({target});",
                ArrayType at =>
                    $"{{" + nl +
                    $"{tab}var length{depth} = unchecked((uint){target}.Length);" + nl +
                    $"{tab}view.WriteUInt32(length{depth});" + nl +
                    $"{tab}for (var {i} = 0; {i} < length{depth}; {i}++) {{" + nl +
                    $"{tab}{tab}{CompileEncodeField(at.MemberType, $"{target}[{i}]", depth + 1, indentDepth + 2)}" + nl +
                    $"{tab}}}" + nl +
                    $"}}",
                MapType mt =>
                    $"view.WriteUInt32(unchecked((uint){target}.Count));" + nl +
                    $"foreach (var kv{depth} in {target}) {{" + nl +
                    $"{tab}{CompileEncodeField(mt.KeyType, $"kv{depth}.Key", depth + 1, indentDepth + 1)}" + nl +
                    $"{tab}{CompileEncodeField(mt.ValueType, $"kv{depth}.Value", depth + 1, indentDepth + 1)}" + nl +
                    $"}}",
                ScalarType st => st.BaseType switch
                {
                    BaseType.Bool => $"view.WriteByte({target});",
                    BaseType.Byte => $"view.WriteByte({target});",
                    BaseType.UInt32 => $"view.WriteUInt32({target});",
                    BaseType.Int32 => $"view.WriteInt32({target});",
                    BaseType.Float32 => $"view.WriteFloat32({target});",
                    BaseType.Float64 => $"view.WriteFloat64({target});",
                    BaseType.String => $"view.WriteString({target});",
                    BaseType.Guid => $"view.WriteGuid({target});",
                    BaseType.UInt16 => $"view.WriteUInt16({target});",
                    BaseType.Int16 => $"view.WriteInt16({target});",
                    BaseType.UInt64 => $"view.WriteUInt64({target});",
                    BaseType.Int64 => $"view.WriteInt64({target});",
                    BaseType.Date => $"view.WriteDate({target});",
                    _ => throw new ArgumentOutOfRangeException()
                },
                DefinedType dt when Schema.Definitions[dt.Name].Kind == AggregateKind.Enum =>
                    $"view.WriteEnum({target});",
                DefinedType dt =>
                    $"{(string.IsNullOrWhiteSpace(Schema.Namespace) ? string.Empty : $"{Schema.Namespace.ToPascalCase()}.")}{dt.Name.ToPascalCase()}.EncodeInto({target}, ref view);",
                _ => throw new InvalidOperationException($"CompileEncodeField: {type}")
            };
        }
    }
}
