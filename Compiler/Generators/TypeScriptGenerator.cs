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
            

            var builder = new StringBuilder();
            builder.AppendLine("      var source = !view;");
            builder.AppendLine("      if (source) view = new PierogiView();");
            foreach (var field in definition.Fields)
            {
                if (field.DeprecatedAttribute.HasValue)
                {
                    continue;
                }
                var code = GetWriteCode(field);
                builder.AppendLine("");
                builder.AppendLine($"      if (message.{field.Name.ToCamelCase()} != null) {{");
                if (definition.Kind == AggregateKind.Message)
                {
                    builder.AppendLine($"        view.writeUint({field.ConstantValue});");
                }
                if (field.IsArray)
                {
                    if ((ScalarType) field.TypeCode == ScalarType.Byte)
                    {
                        builder.AppendLine($"        view.writeBytes(message.{field.Name.ToCamelCase()});");
                    }
                    else
                    {
                       
                        builder.AppendLine($"        view.writeUint(message.{field.Name.ToCamelCase()}.length);");
                        builder.AppendLine($"        for (var i = 0; i < message.{field.Name.ToCamelCase()}.length; i++) {{");
                        builder.AppendLine($"          {code}");
                        builder.AppendLine("        }");
                    }
                }
                else
                {
                    builder.AppendLine($"        {code}");
                }

                if (definition.Kind == AggregateKind.Struct)
                {
                    builder.AppendLine("      } else {");
                    builder.AppendLine($"        throw new Error(\"Missing required field {field.Name.ToCamelCase()}\");");
                }
                builder.AppendLine("      }");
            }
            if (definition.Kind == AggregateKind.Message)
            {
                builder.AppendLine("      view.writeUint(0);");
            }
            builder.AppendLine("");
            builder.AppendLine("      if (source) return view.toArray();");
         
            return builder.ToString();
        }

        /// <summary>
        /// Generate the body of the <c>decode</c> function for the given <see cref="IDefinition"/>.
        /// </summary>
        /// <param name="definition">The definition to generate code for.</param>
        /// <returns>The generated TypeScript <c>decode</c> function body.</returns>
        public string CompileDecode(IDefinition definition)
        {
            var builder = new StringBuilder();
            builder.AppendLine("      if (!(view instanceof PierogiView)) {");
            builder.AppendLine("        view = new PierogiView(view);");
            builder.AppendLine("      }");
            builder.AppendLine("");
            if (definition.Kind == AggregateKind.Message)
            {
                builder.AppendLine($"      let message: I{definition.Name} = {{}};");
                builder.AppendLine("      while (true) {");
                builder.AppendLine("        switch (view.readUint()) {");
                builder.AppendLine("          case 0:");
                builder.AppendLine("            return message;");
                builder.AppendLine("");
            } else if (definition.Kind == AggregateKind.Struct)
            {
                builder.AppendLine($"      var message: I{definition.Name} = {{");
            }
            var indent  = definition.Kind switch
            {
                AggregateKind.Struct => "      ",
                AggregateKind.Message => "            ",
                _ => "   "
            };
            foreach (var field in definition.Fields)
            {

                var code = GetReadCode(field);
                
                if (definition.Kind == AggregateKind.Message)
                {
                    builder.AppendLine($"          case {field.ConstantValue}:");
                }
                if (field.IsArray)
                {
                    if (field.DeprecatedAttribute.HasValue)
                    {
                        if ((ScalarType) field.TypeCode == ScalarType.Byte)
                        {
                            builder.AppendLine($"      view.readBytes();");
                        }
                        else
                        {
                            builder.AppendLine($"{indent}var length = view.readUint();");
                            builder.AppendLine($"{indent}while (length-- > 0) {code};");
                        }
                    }
                    else
                    {
                        if ((ScalarType)field.TypeCode == ScalarType.Byte)
                        {
                            if (definition.Kind == AggregateKind.Struct)
                            {
                                builder.AppendLine($"          {field.Name.ToCamelCase()}: view.readBytes(),");
                            }
                            else
                            {
                                builder.AppendLine($"{indent}message.{field.Name.ToCamelCase()} = view.readBytes();");
                            }

                        }
                        else
                        {
                            if (definition.Kind == AggregateKind.Struct)
                            {
                                builder.AppendLine($"          {field.Name.ToCamelCase()}: (");
                                builder.AppendLine($"               () => {{");
                                builder.AppendLine($"                   let length = view.readUint();");
                                builder.AppendLine($"                   const collection = new Array<{GetTsType(field, false)}>(length);");
                                builder.AppendLine($"                   for (var i = 0; i < length; i++) collection[i] = {code};");
                                builder.AppendLine($"                   return collection;");
                                builder.AppendLine($"               }}");
                                builder.AppendLine($"           )(),");
                            }
                            else
                            {
                                builder.AppendLine($"{indent}let length = view.readUint();");
                                builder.AppendLine($"{indent}message.{field.Name.ToCamelCase()} = new Array<{GetTsType(field, false)}>(length);");
                                builder.AppendLine($"{indent}for (var i = 0; i < length; i++) message.{field.Name.ToCamelCase()}[i] = {code};");
                            }

                        }
                    }
                }
                else
                {
                    if (field.DeprecatedAttribute.HasValue)
                    {
                        builder.AppendLine($"{indent}{code};");
                    }
                    else
                    {
                        if (definition.Kind == AggregateKind.Struct)
                        {
                            builder.AppendLine($"          {field.Name.ToCamelCase()}: {code},");
                        }
                        else
                        {
                            builder.AppendLine($"{indent}message.{field.Name.ToCamelCase()} = {code};");
                        }
                        
                    }
                }

                if (definition.Kind == AggregateKind.Message)
                {
                    builder.AppendLine("            break;");
                    builder.AppendLine("");
                }
            }
            if (definition.Kind == AggregateKind.Message)
            {
                builder.AppendLine("          default:");
                builder.AppendLine("            throw new Error(\"Attempted to parse invalid message\");");
                builder.AppendLine("        }");
                builder.AppendLine("      }");
                builder.AppendLine("    }");
                builder.AppendLine("  };");
            }
            else
            {
                if (definition.Kind == AggregateKind.Struct)
                {
                    builder.AppendLine("      };");
                    builder.AppendLine("");
                }
                builder.AppendLine("      return message;");
                builder.AppendLine("    }");
                builder.AppendLine("  };");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Generate a "write" call for the given <see cref="IField"/>.
        /// </summary>
        /// <param name="field">The field to generate code for.</param>
        /// <returns>The generated "write" call.</returns>
        string GetWriteCode(IField field)
        {
            var method = field.TypeCode switch 
            {
                _ when field.TypeCode < 0 => (ScalarType)field.TypeCode switch
                {
                    ScalarType.Bool => "writeByte",
                    ScalarType.Byte => "writeByte",
                    ScalarType.UInt => "writeUint",
                    ScalarType.Int => "writeInt",
                    ScalarType.Float => "writeFloat",
                    ScalarType.String => "writeString",
                    ScalarType.Guid => "writeGuid",
                    _ => string.Empty
                },
                _ => _schema.Definitions.ElementAt(field.TypeCode) switch
                {
                    var f when f.Kind == AggregateKind.Enum => "writeEnum",
                    _  => "encode",
                },
            };
            var index = field.IsArray ? "[i]" : "";
            var fieldName = $"message.{field.Name.ToCamelCase()}";
            var code =
                method == "encode"
                    ? $"{_schema.Definitions.ElementAt(field.TypeCode).Name}.encode({fieldName}{index}, view);"
                    : $"view.{method}({fieldName}{index});";
            return code;
        }

        /// <summary>
        /// Generate a "read" call for the given <see cref="IField"/>.
        /// </summary>
        /// <param name="field">The field to generate code for.</param>
        /// <returns>The generated "read" call.</returns>
        private string GetReadCode(in IField field)
        {
            var code = field.TypeCode switch
            {
                _ when field.TypeCode < 0 => (ScalarType)field.TypeCode switch
                {
                    ScalarType.Bool => "!!view.readByte()",
                    ScalarType.Byte => "view.readByte()",
                    ScalarType.UInt => "view.readUint()",
                    ScalarType.Int => "view.readInt()",
                    ScalarType.Float => "view.readFloat()",
                    ScalarType.String => "view.readString()",
                    ScalarType.Guid => "view.readGuid()",
                    _ => string.Empty
                },
                _ => string.Empty
            };
            if (string.IsNullOrWhiteSpace(code))
            {
                var f = _schema.Definitions.ElementAt(field.TypeCode);
                if (f.Kind == AggregateKind.Enum)
                {
                    code = $"view.readUint() as {f.Name}";
                }
                else
                {
                    code = $"{f.Name}.decode(view)";
                }
            }
            return code;
        }


        /// <summary>
        /// Generate a TypeScript type name for the given <see cref="IField"/>.
        /// </summary>
        /// <param name="field">The field to generate code for.</param>
        /// <param name="formatArray">If set to "false", omit the "[]" from array type names.</param>
        /// <returns>The TypeScript type name.</returns>
        private string GetTsType(in IField field, bool formatArray = true)
        {
            var type = field.TypeCode switch
            {
                _ when field.TypeCode < 0 => (ScalarType)field.TypeCode switch
                {
                    ScalarType.Bool => formatArray && field.IsArray ? "boolean[]" : "boolean",
                    ScalarType.Byte => formatArray && field.IsArray ? "Uint8Array" : "number",
                    ScalarType.UInt => formatArray && field.IsArray ? "number[]" : "number",
                    ScalarType.Int => formatArray && field.IsArray ? "number[]" : "number",
                    ScalarType.Float => formatArray && field.IsArray ? "number[]" : "number",
                    ScalarType.String => formatArray && field.IsArray ? "string[]" : "string",
                    ScalarType.Guid => formatArray && field.IsArray ? "string[]" : "string",
                    _ => string.Empty
                },
                _ => string.Empty
            };
            if (string.IsNullOrWhiteSpace(type))
            {
                var f = _schema.Definitions.ElementAt(field.TypeCode);
                type = f.Kind != AggregateKind.Enum ? $"I{f.Name}" : f.Name;
                if (formatArray && field.IsArray)
                {
                    type = $"{type}[]";
                }
            }
            return type;
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

            foreach (var definition in _schema.Definitions)
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

                        var type = GetTsType(field);
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