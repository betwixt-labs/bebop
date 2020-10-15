using System;
using System.IO;
using System.Linq;
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
                if (field.Type is ArrayType at)
                {
                    if (at.IsBytes())
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
            }
            else if (definition.Kind == AggregateKind.Struct)
            {
                builder.AppendLine($"      var message: I{definition.Name} = {{");
            }
            var indent = definition.Kind switch
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
                if (field.Type is ArrayType at)
                {
                    if (field.DeprecatedAttribute.HasValue)
                    {
                        if (at.IsBytes())
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
                        if (at.IsBytes())
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
                                builder.AppendLine($"                   const collection = new {GetTsType(field.Type)}(length);");
                                builder.AppendLine($"                   for (var i = 0; i < length; i++) collection[i] = {code};");
                                builder.AppendLine($"                   return collection;");
                                builder.AppendLine($"               }}");
                                builder.AppendLine($"           )(),");
                            }
                            else
                            {
                                builder.AppendLine($"{indent}let length = view.readUint();");
                                builder.AppendLine($"{indent}message.{field.Name.ToCamelCase()} = new {GetTsType(field.Type)}(length);");
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
            IType t = field.Type;
            var isArray = false;
            if (t is ArrayType at)
            {
                t = at.MemberType;
                isArray = true;
                if (t is ArrayType) throw new NotSupportedException("nested array");
            }

            var method = t switch
            {
                ScalarType st => st.BaseType switch
                {
                    BaseType.Bool => "writeByte",
                    BaseType.Byte => "writeByte",
                    BaseType.UInt => "writeUint",
                    BaseType.Int => "writeInt",
                    BaseType.Float => "writeFloat",
                    BaseType.String => "writeString",
                    BaseType.Guid => "writeGuid",
                    _ => string.Empty,
                },
                DefinedType dt => _schema.Definitions[dt.Name].Kind switch
                {
                    AggregateKind.Enum => "writeEnum",
                    _ => "encode",
                },
                _ => string.Empty,
            };
            var index = isArray ? "[i]" : "";
            var fieldName = $"message.{field.Name.ToCamelCase()}";
            var code =
                method == "encode"
                    ? $"{(t as DefinedType).Name}.encode({fieldName}{index}, view);"
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
            IType t = field.Type;
            var isArray = false;
            if (t is ArrayType at)
            {
                t = at.MemberType;
                isArray = true;
                if (t is ArrayType) throw new NotSupportedException("nested array");
            }


            var code = t switch
            {
                ScalarType st => st.BaseType switch
                {
                    BaseType.Bool => "!!view.readByte()",
                    BaseType.Byte => "view.readByte()",
                    BaseType.UInt => "view.readUint()",
                    BaseType.Int => "view.readInt()",
                    BaseType.Float => "view.readFloat()",
                    BaseType.String => "view.readString()",
                    BaseType.Guid => "view.readGuid()",
                    _ => string.Empty
                },
                _ => string.Empty
            };
            if (string.IsNullOrWhiteSpace(code))
            {
                var f = _schema.Definitions[(t as DefinedType).Name];
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
                }
            }
            if (!string.IsNullOrWhiteSpace(_schema.Package))
            {
                builder.AppendLine("}");
            }
            builder.AppendLine("");


            return builder.ToString().TrimEnd();
        }
    }
}