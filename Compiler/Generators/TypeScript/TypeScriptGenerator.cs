using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Compiler.Meta;
using Compiler.Meta.Extensions;
using Compiler.Meta.Interfaces;

namespace Compiler.Generators.TypeScript
{
    public class TypeScriptGenerator : Generator
    {
        public TypeScriptGenerator(ISchema schema) : base(schema) { }


        private string FormatDocumentation(string documentation, int spaces)
        {
            var builder = new IndentedStringBuilder();
            builder.Indent(spaces);
            builder.AppendLine("/**");
            builder.Indent(1);
            foreach (var line in documentation.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None))
            {
                builder.AppendLine($"* {line}");
            }
            builder.AppendLine("*/");
            return builder.ToString();
        }

        /// <summary>
        /// Generate the body of the <c>encode</c> function for the given <see cref="IDefinition"/>.
        /// </summary>
        /// <param name="definition">The definition to generate code for.</param>
        /// <returns>The generated TypeScript <c>encode</c> function body.</returns>
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
            var builder = new IndentedStringBuilder(6);
            builder.AppendLine($"const pos = view.reserveMessageLength();");
            builder.AppendLine($"const start = view.length;");
            foreach (var field in definition.Fields)
            {
                if (field.DeprecatedAttribute.HasValue)
                {
                    continue;
                }
                builder.AppendLine($"if (message.{field.Name.ToCamelCase()} != null) {{");
                builder.AppendLine($"  view.writeByte({field.ConstantValue});");
                builder.AppendLine($"  {CompileEncodeField(field.Type, $"message.{field.Name.ToCamelCase()}")}");
                builder.AppendLine($"}}");
            }
            builder.AppendLine("view.writeByte(0);");
            builder.AppendLine("const end = view.length;");
            builder.AppendLine("view.fillMessageLength(pos, end - start);");
            return builder.ToString();
        }

        private string CompileEncodeStruct(IDefinition definition)
        {
            var builder = new IndentedStringBuilder(6);
            foreach (var field in definition.Fields)
            {
                builder.AppendLine(CompileEncodeField(field.Type, $"message.{field.Name.ToCamelCase()}"));
            }
            return builder.ToString();
        }

        private string CompileEncodeField(IType type, string target, int depth = 0)
        {
            var indent = new string(' ', (depth + 4) * 2);
            var i = GeneratorUtils.LoopVariable(depth);
            return type switch
            {
                ArrayType at when at.IsBytes() => $"view.writeBytes({target});",
                ArrayType at => $"var length{depth} = {target}.length;\n" + indent +
                    $"view.writeUint32(length{depth});\n" + indent +
                    $"for (var {i} = 0; {i} < length{depth}; {i}++) {{\n" + indent +
                    $"  {CompileEncodeField(at.MemberType, $"{target}[{i}]", depth + 1)}\n" + indent + "}",
                ScalarType st => st.BaseType switch
                {
                    BaseType.Bool => $"view.writeByte(Number({target}));",
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
                    _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                },
                DefinedType dt when Schema.Definitions[dt.Name].Kind == AggregateKind.Enum =>
                    $"view.writeEnum({target});",
                DefinedType dt => $"{dt.Name}.encodeInto({target}, view)",
                _ => throw new InvalidOperationException($"CompileEncodeField: {type}")
            };
        }

        /// <summary>
        /// Generate the body of the <c>decode</c> function for the given <see cref="IDefinition"/>.
        /// </summary>
        /// <param name="definition">The definition to generate code for.</param>
        /// <returns>The generated TypeScript <c>decode</c> function body.</returns>
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
        /// <returns>The generated TypeScript <c>decode</c> function body.</returns>
        private string CompileDecodeMessage(IDefinition definition)
        {
            var builder = new IndentedStringBuilder(6);
            builder.AppendLine("if (!(view instanceof BebopView)) {");
            builder.AppendLine("  view = new BebopView(view);");
            builder.AppendLine("}");
            builder.AppendLine("");
            builder.AppendLine($"let message: I{definition.Name} = {{}};");
            builder.AppendLine("const length = view.readMessageLength();");
            builder.AppendLine("const end = view.index + length;");
            builder.AppendLine("while (true) {");
            builder.Indent(2);
            builder.AppendLine("switch (view.readByte()) {");
            builder.AppendLine("  case 0:");
            builder.AppendLine("    return message;");
            builder.AppendLine("");
            foreach (var field in definition.Fields)
            {
                builder.AppendLine($"  case {field.ConstantValue}:");
                builder.AppendLine($"    message.{field.Name.ToCamelCase()} = {CompileDecodeField(field.Type)};");
                builder.AppendLine("    break;");
                builder.AppendLine("");
            }
            builder.AppendLine("  default:");
            builder.AppendLine("    view.index = end;");
            builder.AppendLine("    return message;");
            builder.AppendLine("}");
            builder.Dedent(2);
            builder.AppendLine("}");
            return builder.ToString();
        }
        
        private string CompileDecodeStruct(IDefinition definition)
        {
            var builder = new IndentedStringBuilder(6);
            builder.AppendLine("if (!(view instanceof BebopView)) {");
            builder.AppendLine("  view = new BebopView(view);");
            builder.AppendLine("}");
            builder.AppendLine("");
            builder.AppendLine($"var message: I{definition.Name} = {{");
            foreach (var field in definition.Fields)
            {
                builder.AppendLine($"  {field.Name.ToCamelCase()}: {CompileDecodeField(field.Type)},");
            }
            builder.AppendLine("};");
            builder.AppendLine("return message;");
            return builder.ToString();
        }

        private string CompileDecodeField(IType type)
        {
            return type switch
            {
                ArrayType at when at.IsBytes() => "view.readBytes()",
                ArrayType at => @$"(() => {{
                        let length = view.readUint32();
                        const collection = new {TypeName(at)}(length);
                        for (var i = 0; i < length; i++) collection[i] = {CompileDecodeField(at.MemberType)};
                        return collection;
                    }})()",
                ScalarType st => st.BaseType switch
                {
                    BaseType.Bool => "!!view.readByte()",
                    BaseType.Byte => "view.readByte()",
                    BaseType.UInt16 => "view.readUint16()",
                    BaseType.Int16 => "view.readInt16()",
                    BaseType.UInt32 => "view.readUint32()",
                    BaseType.Int32 => "view.readInt32()",
                    BaseType.UInt64 => "view.readUint64()",
                    BaseType.Int64 => "view.readInt64()",
                    BaseType.Float32 => "view.readFloat32()",
                    BaseType.Float64 => "view.readFloat64()",
                    BaseType.String => "view.readString()",
                    BaseType.Guid => "view.readGuid()",
                    _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                },
                DefinedType dt when Schema.Definitions[dt.Name].Kind == AggregateKind.Enum =>
                    $"view.readUint32() as {dt.Name}",
                DefinedType dt => $"{dt.Name}.decode(view)",
                _ => throw new InvalidOperationException($"CompileDecodeField: {type}")
            };
        }

        /// <summary>
        /// Generate a TypeScript type name for the given <see cref="IType"/>.
        /// </summary>
        /// <param name="type">The field type to generate code for.</param>
        /// <returns>The TypeScript type name.</returns>
        private string TypeName(in IType type)
        {
            switch (type)
            {
                case ScalarType st:
                    return st.BaseType switch
                    {
                        BaseType.Bool => "boolean",
                        BaseType.Byte or BaseType.UInt16 or BaseType.Int16 or BaseType.UInt32 or BaseType.Int32 or
                            BaseType.Float32 or BaseType.Float64 => "number",
                        BaseType.UInt64 or BaseType.Int64 => "bigint",
                        BaseType.String or BaseType.Guid => "string",
                        _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                    };
                case ArrayType at when at.IsBytes():
                    return "Uint8Array";
                case ArrayType at:
                    return $"Array<{TypeName(at.MemberType)}>";
                case DefinedType dt:
                    var isEnum = Schema.Definitions[dt.Name].Kind == AggregateKind.Enum;
                    return (isEnum ? "" : "I") + dt.Name;
            }
            throw new InvalidOperationException($"GetTypeName: {type}");
        }

        /// <summary>
        /// Generate code for a Bebop schema.
        /// </summary>
        /// <returns>The generated code.</returns>
        public override string Compile()
        {
            var builder = new StringBuilder();
            builder.AppendLine("import { BebopView } from \"./BebopView\";");
            builder.AppendLine("");
            if (!string.IsNullOrWhiteSpace(Schema.Namespace))
            {
                builder.AppendLine($"export namespace {Schema.Namespace} {{");
            }

            foreach (var definition in Schema.Definitions.Values)
            {
                if(!string.IsNullOrWhiteSpace(definition.Documentation))
                {
                    builder.Append(FormatDocumentation(definition.Documentation, 2));
                }
                if (definition.Kind == AggregateKind.Enum)
                {
                    builder.AppendLine($"  export enum {definition.Name} {{");
                    for (var i = 0; i < definition.Fields.Count; i++)
                    {
                        var field = definition.Fields.ElementAt(i);
                        var comma = i + 1 < definition.Fields.Count ? "," : "";
                        if (!string.IsNullOrWhiteSpace(field.Documentation))
                        {
                            builder.Append(FormatDocumentation(field.Documentation, 5));
                        }
                        builder.AppendLine($"      {field.Name} = {field.ConstantValue}{comma}");
                    }
                    builder.AppendLine("  }");
                }

                if (definition.Kind == AggregateKind.Message || definition.Kind == AggregateKind.Struct)
                {
                    builder.AppendLine($"  export interface I{definition.Name} {{");
                    for (var i = 0; i < definition.Fields.Count; i++)
                    {
                        var field = definition.Fields.ElementAt(i);
                        var type = TypeName(field.Type);
                        var comma = i + 1 < definition.Fields.Count ? "," : "";
                        if (!string.IsNullOrWhiteSpace(field.Documentation))
                        {
                            builder.Append(FormatDocumentation(field.Documentation, 3));
                        }
                        if (field.DeprecatedAttribute.HasValue && !string.IsNullOrWhiteSpace(field.DeprecatedAttribute.Value.Message))
                        {
                            builder.AppendLine("    /**");
                            builder.AppendLine($"     * @deprecated {field.DeprecatedAttribute.Value.Message}");
                            builder.AppendLine($"     */");
                        }
                        builder.AppendLine($"    {(definition.IsReadOnly ? "readonly " : "")}{field.Name.ToCamelCase()}{(definition.Kind == AggregateKind.Message ? "?" : "")}: {type}");
                    }
                    builder.AppendLine("  }");
                    builder.AppendLine("");

                    builder.AppendLine($"  export const {definition.Name} = {{");
                    builder.AppendLine($"    encode(message: I{definition.Name}): Uint8Array {{");
                    builder.AppendLine("      const view = new BebopView();");
                    builder.AppendLine("      this.encodeInto(message, view);");
                    builder.AppendLine("      return view.toArray();");
                    builder.AppendLine("    },");
                    builder.AppendLine("");
                    builder.AppendLine($"    encodeInto(message: I{definition.Name}, view: BebopView): void {{");
                    builder.Append(CompileEncode(definition));
                    builder.AppendLine("    },");
                    builder.AppendLine("");

                    builder.AppendLine($"    decode(view: BebopView | Uint8Array): I{definition.Name} {{");
                    builder.Append(CompileDecode(definition));
                    builder.AppendLine("    },");
                    builder.AppendLine("  };");
                }
            }
            if (!string.IsNullOrWhiteSpace(Schema.Namespace))
            {
                builder.AppendLine("}");
            }

            return builder.ToString();
        }

        public override void WriteAuxiliaryFiles(string outputPath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream("Compiler.Generators.TypeScript.BebopView.ts")!;
            using StreamReader reader = new StreamReader(stream);
            string result = reader.ReadToEnd();
            File.WriteAllText(Path.Join(outputPath, "BebopView.ts"), result);
        }
    }
}
