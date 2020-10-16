using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Compiler.Meta;
using Compiler.Meta.Extensions;
using Compiler.Meta.Interfaces;

namespace Compiler.Generators
{
    public class TypeScriptGenerator : IGenerator
    {
        private ISchema _schema;

        /// <summary>
        /// Generate the body of the <c>encode</c> function for the given <see cref="IDefinition"/>.
        /// </summary>
        /// <param name="definition">The definition to generate code for.</param>
        /// <returns>The generated TypeScript <c>encode</c> function body.</returns>
        public string CompileEncode(IDefinition definition)
        {
            switch (definition.Kind)
            {
                case AggregateKind.Message:
                    return CompileEncodeMessage(definition);
                case AggregateKind.Struct:
                    return CompileEncodeStruct(definition);
                default:
                    throw new InvalidOperationException($"invalid CompileEncode kind: {definition.Kind} in {definition}");
            }
        }

        private string CompileEncodeMessage(IDefinition definition)
        { 
            var builder = new StringBuilder();
            builder.AppendLine("      var source = !view;");
            builder.AppendLine("      if (source) view = new PierogiView();");
            foreach (var field in definition.Fields)
            {
                if (field.DeprecatedAttribute.HasValue)
                {
                    continue;
                }
                builder.AppendLine("");
                builder.AppendLine($"      if (message.{field.Name.ToCamelCase()} != null) {{");
                builder.AppendLine($"        view.writeUint({field.ConstantValue});");
                builder.AppendLine($"        {CompileEncodeField(field.Type, $"message.{field.Name.ToCamelCase()}")}");
                builder.AppendLine($"      }}");
            }
            builder.AppendLine("      view.writeUint(0);");
            builder.AppendLine("");
            builder.AppendLine("      if (source) return view.toArray();");
            return builder.ToString();
        }

        private string CompileEncodeStruct(IDefinition definition)
        {
            var builder = new StringBuilder();
            builder.AppendLine("      var source = !view;");
            builder.AppendLine("      if (source) view = new PierogiView();");
            foreach (var field in definition.Fields)
            {
                builder.AppendLine("");
                builder.AppendLine($"      if (message.{field.Name.ToCamelCase()} != null) {{");
                builder.AppendLine($"        view.writeUint({field.ConstantValue});");
                builder.AppendLine($"        {CompileEncodeField(field.Type, $"message.{field.Name.ToCamelCase()}")}");
                builder.AppendLine($"      }} else throw new Error(\"Missing required field {field.Name.ToCamelCase()}\");");
            }
            builder.AppendLine("");
            builder.AppendLine("      if (source) return view.toArray();");
            return builder.ToString();
        }

        private string CompileEncodeField(IType type, string target, int depth = 0)
        {
            switch (type)
            {
                case ArrayType at when at.IsBytes():
                    return $"view.writeBytes({target});";
                case ArrayType at:
                    var indent = new string(' ', (depth + 2) * 4);
                    var i = LoopVariable(depth);
                    return $"var length{depth} = {target}.length;\n"
                        + indent + $"view.writeUint(length{depth});\n"
                        + indent + $"for (var {i} = 0; {i} < length{depth}; {i}++) {{\n"
                        + indent + $"    {CompileEncodeField(at.MemberType, $"{target}[{i}]", depth + 1)}\n"
                        + indent + "}";
                case ScalarType st:
                    switch (st.BaseType)
                    {
                        case BaseType.Bool: return $"view.writeByte({target});";
                        case BaseType.Byte: return $"view.writeByte({target});";
                        case BaseType.UInt: return $"view.writeUint({target});";
                        case BaseType.Int: return $"view.writeInt({target});";
                        case BaseType.Float: return $"view.writeFloat({target});";
                        case BaseType.String: return $"view.writeString({target});";
                        case BaseType.Guid: return $"view.writeGuid({target});";
                    }
                    break;
                case DefinedType dt when _schema.Definitions[dt.Name].Kind == AggregateKind.Enum:
                    return $"view.writeEnum({target});";
                case DefinedType dt:
                    return $"{dt.Name}.encode({target}, view)";
            }
            throw new InvalidOperationException($"CompileEncodeField: {type}");
        }

        private string LoopVariable(int depth)
        {
            return depth switch
            {
                0 => "i",
                1 => "j",
                2 => "k",
                3 => "l",
                _ => $"i{depth}",
            };
        }

        /// <summary>
        /// Generate the body of the <c>decode</c> function for the given <see cref="IDefinition"/>.
        /// </summary>
        /// <param name="definition">The definition to generate code for.</param>
        /// <returns>The generated TypeScript <c>decode</c> function body.</returns>
        public string CompileDecode(IDefinition definition)
        {
            switch (definition.Kind)
            {
                case AggregateKind.Message:
                    return CompileDecodeMessage(definition);
                case AggregateKind.Struct:
                    return CompileDecodeStruct(definition);
                default:
                    throw new InvalidOperationException($"invalid CompileDecode kind: {definition.Kind} in {definition}");
            }
        }

        /// <summary>
        /// Generate the body of the <c>decode</c> function for the given <see cref="IDefinition"/>,
        /// given that its "kind" is Message.
        /// </summary>
        /// <param name="definition">The message definition to generate code for.</param>
        /// <returns>The generated TypeScript <c>decode</c> function body.</returns>
        private string CompileDecodeMessage(IDefinition definition)
        {
            var builder = new StringBuilder();
            builder.AppendLine("      if (!(view instanceof PierogiView)) {");
            builder.AppendLine("        view = new PierogiView(view);");
            builder.AppendLine("      }");
            builder.AppendLine("");
            builder.AppendLine($"      let message: I{definition.Name} = {{}};");
            builder.AppendLine("      while (true) {");
            builder.AppendLine("        switch (view.readUint()) {");
            builder.AppendLine("          case 0:");
            builder.AppendLine("            return message;");
            builder.AppendLine("");
            foreach (var field in definition.Fields)
            {
                builder.AppendLine($"          case {field.ConstantValue}:");
                builder.AppendLine($"            message.{field.Name.ToCamelCase()} = {CompileDecodeField(field.Type)};");
                builder.AppendLine("            break;");
                builder.AppendLine("");
            }
            builder.AppendLine("          default:");
            builder.AppendLine("            throw new Error(\"Attempted to parse invalid message\");");
            builder.AppendLine("        }");
            builder.AppendLine("      }");
            builder.AppendLine("    },");
            return builder.ToString();
        }
        
        private string CompileDecodeStruct(IDefinition definition)
        {
            var builder = new StringBuilder();
            builder.AppendLine("      if (!(view instanceof PierogiView)) {");
            builder.AppendLine("        view = new PierogiView(view);");
            builder.AppendLine("      }");
            builder.AppendLine("");
            builder.AppendLine($"      var message: I{definition.Name} = {{");
            foreach (var field in definition.Fields)
            {
                builder.AppendLine($"          {field.Name.ToCamelCase()}: {CompileDecodeField(field.Type)},");
            }
            builder.AppendLine("      };");
            builder.AppendLine("      return message;");
            builder.AppendLine("    }");
            return builder.ToString();
        }

        private string CompileDecodeField(IType type)
        {
            switch (type)
            {
                case ArrayType at when at.IsBytes():
                    return "view.readBytes()";
                case ArrayType at:
                    return @$"(() => {{
                        let length = view.readUint();
                        const collection = new {GetTsType(at)}(length);
                        for (var i = 0; i < length; i++) collection[i] = {CompileDecodeField(at.MemberType)};
                        return collection;
                    }})()";
                case ScalarType st:
                    switch (st.BaseType)
                    {
                        case BaseType.Bool: return "!!view.readByte()";
                        case BaseType.Byte: return "view.readByte()";
                        case BaseType.UInt: return "view.readUint()";
                        case BaseType.Int: return "view.readInt()";
                        case BaseType.Float: return "view.readFloat()";
                        case BaseType.String: return "view.readString()";
                        case BaseType.Guid: return "view.readGuid()";
                    }
                    break;
                case DefinedType dt when _schema.Definitions[dt.Name].Kind == AggregateKind.Enum:
                    return $"view.readUint() as {dt.Name}";
                case DefinedType dt:
                    return $"{dt.Name}.decode(view)";
            }
            throw new InvalidOperationException($"CompileDecodeField: {type}");
        }

        /// <summary>
        /// Generate a TypeScript type name for the given <see cref="IType"/>.
        /// </summary>
        /// <param name="type">The field type to generate code for.</param>
        /// <returns>The TypeScript type name.</returns>
        private string GetTsType(in IType type)
        {
            switch (type)
            {
                case ScalarType st:
                    switch (st.BaseType)
                    {
                        case BaseType.Bool:
                            return "boolean";
                        case BaseType.Byte:
                        case BaseType.UInt:
                        case BaseType.Int:
                        case BaseType.Float:
                            return "number";
                        case BaseType.String:
                        case BaseType.Guid:
                            return "string";
                    }
                    break;
                case ArrayType at:
                    return at.IsBytes() ? "Uint8Array" : $"Array<{GetTsType(at.MemberType)}>";
                case DefinedType dt:
                    var isEnum = _schema.Definitions[dt.Name].Kind == AggregateKind.Enum;
                    return (isEnum ? "" : "I") + dt.Name;
            }
            throw new InvalidOperationException($"GetTsType: {type}");
        }

        /// <summary>
        /// Generate code for a Pierogi schema.
        /// </summary>
        /// <returns>The generated code.</returns>
        public string Compile(ISchema schema)
        {
            _schema = schema;

            var builder = new StringBuilder();
            builder.AppendLine("import { PierogiView } from \"./PierogiView\";");
            builder.AppendLine("");
            if (!string.IsNullOrWhiteSpace(_schema.Package))
            {
                builder.AppendLine($"export namespace {_schema.Package} {{");
            }

            foreach (var definition in _schema.Definitions.Values)
            {
                if (definition.Kind == AggregateKind.Enum)
                {
                    builder.AppendLine($"  export enum {definition.Name} {{");
                    for (var i = 0; i < definition.Fields.Count; i++)
                    {
                        var field = definition.Fields.ElementAt(i);
                        builder.AppendLine(
                            $"      {field.Name} = {field.ConstantValue}{(i + 1 < definition.Fields.Count ? "," : "")}");
                    }
                    builder.AppendLine("  }");
                }

                if (definition.Kind == AggregateKind.Message || definition.Kind == AggregateKind.Struct)
                {
                    builder.AppendLine($"  export interface I{definition.Name} {{");
                    for (var i = 0; i < definition.Fields.Count; i++)
                    {
                        var field = definition.Fields.ElementAt(i);

                        var type = GetTsType(field.Type);
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
                    builder.AppendLine("");
                    builder.AppendLine($"    encode(message: I{definition.Name}, view: PierogiView): Uint8Array | void {{");
                    builder.AppendLine(CompileEncode(definition));
                    builder.AppendLine("    },");
                    builder.AppendLine("");

                    builder.AppendLine($"    decode(view: PierogiView | Uint8Array): I{definition.Name} {{");
                    builder.AppendLine(CompileDecode(definition));
                    builder.AppendLine("  };");
                }
            }
            if (!string.IsNullOrWhiteSpace(_schema.Package))
            {
                builder.AppendLine("}");
            }
            builder.AppendLine("");


            return builder.ToString().TrimEnd();
        }

        public string OutputFileName(ISchema schema)
        {
            return schema.Package + ".ts";
        }

        public void WriteAuxiliaryFiles(string outputPath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream("Compiler.Generators.Resources.PierogiView.ts");
            using StreamReader reader = new StreamReader(stream);
            string result = reader.ReadToEnd();
            File.WriteAllText(Path.Join(outputPath, "PierogiView.ts"), result);
        }
    }
}