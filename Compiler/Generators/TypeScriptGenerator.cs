using System;
using System.IO;
using System.Linq;
using System.Text;
using Compiler.Meta;
using Compiler.Meta.Extensions;
using Compiler.Meta.Interfaces;

namespace Compiler.Generators
{
    public class TypeScriptGenerator
    {
        private readonly ISchema _schema;

        public TypeScriptGenerator(ISchema schema)
        {
            _schema = schema;
        }

        public string CompileEncode(IDefinition definition)
        {
            return string.Empty;
        }

        public string CompileDecode(IDefinition definition)
        {
            string Code(IField field)
            {
                var code = field.TypeCode switch
                {
                    _ when field.TypeCode < 0 => (ScalarType) field.TypeCode switch
                    {
                        ScalarType.Bool => "!!view.readByte()",
                        ScalarType.Byte => "view.readByte()",
                        ScalarType.UInt => "view.readInt()",
                        ScalarType.Int => "view.readUint()",
                        ScalarType.Float => "view.readFloat()",
                        ScalarType.String => "view.readString()",
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
                builder.AppendLine($"      var message: I{definition.Name};");
                builder.AppendLine($"      message = (() => {{ }}) as unknown as I{definition.Name};");
                builder.AppendLine("");
            }
            var indent  = definition.Kind switch
            {
                AggregateKind.Struct => "      ",
                AggregateKind.Message => "            ",
                _ => "   "
            };
            foreach (var field in definition.Fields)
            {

                var code = Code(field);
                
                if (definition.Kind == AggregateKind.Message)
                {
                    builder.AppendLine($"          case {field.ConstantValue}:");
                }
                if (field.IsArray)
                {
                    if (field.IsDeprecated)
                    {
                        if ((ScalarType) field.TypeCode == ScalarType.Byte)
                        {
                            builder.AppendLine($"{indent}view.readBytes();");
                        }
                        else
                        {
                            builder.AppendLine($"{indent}var length = view.readUint();");
                            builder.AppendLine($"{indent}while (length-- > 0) {code};");
                        }
                    }
                    else
                    {
                        if ((ScalarType) field.TypeCode == ScalarType.Byte)
                        {
                            builder.AppendLine($"{indent}message.{field.Name.ToCamelCase()} = view.readBytes();");
                        }
                        else
                        {
                            builder.AppendLine($"{indent}var length = view.readUint();");
                            builder.AppendLine($"{indent}message.{field.Name.ToCamelCase()} = [];");
                            builder.AppendLine($"{indent}while (length-- > 0) message.{field.Name.ToCamelCase()}.push({code});");
                        }
                    }
                }
                else
                {
                    if (field.IsDeprecated)
                    {
                        builder.AppendLine($"{indent}{code};");
                    }
                    else
                    {
                        builder.AppendLine($"{indent}message.{field.Name.ToCamelCase()} = {code};");
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
                builder.AppendLine("      return message;");
                builder.AppendLine("    }");
                builder.AppendLine("  };");
            }
    
            return builder.ToString();
        }

        public string Compile()
        {
            string Type(IField field)
            {
                var type = field.TypeCode switch
                {
                    _ when field.TypeCode < 0 => (ScalarType) field.TypeCode switch
                    {
                        ScalarType.Bool => field.IsArray ? "boolean[]" : "boolean",
                        ScalarType.Byte => field.IsArray ? "Uint8Array" : "number",
                        ScalarType.UInt => field.IsArray ? "number[]" : "number",
                        ScalarType.Int => field.IsArray ? "number[]" : "number",
                        ScalarType.Float => field.IsArray ? "number[]" : "number",
                        ScalarType.String => field.IsArray ? "string[]" : "string",
                        _ => string.Empty
                    },
                    _ => string.Empty
                };
                if (string.IsNullOrWhiteSpace(type))
                {
                    var f = _schema.Definitions.ElementAt(field.TypeCode);
                    type = f.Kind != AggregateKind.Enum ? $"I{f.Name}" : f.Name;
                    if (field.IsArray)
                    {
                        type = $"{type}[]";
                    }
                }
                return type;
            }

           
            var builder = new StringBuilder();
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
                            $"    {field.Name} = {field.ConstantValue}{(i + 1 < definition.Fields.Count ? "," : "")}");
                    }
                    builder.AppendLine("  }");
                }

                if (definition.Kind == AggregateKind.Message || definition.Kind == AggregateKind.Struct)
                {
                    builder.AppendLine($"  export interface I{definition.Name} {{");
                    for (var i = 0; i < definition.Fields.Count; i++)
                    {
                        var field = definition.Fields.ElementAt(i);

                        var type = Type(field);
                        builder.AppendLine($"    {field.Name.ToCamelCase()}{(definition.Kind == AggregateKind.Message ? "?" : "")}: {type}");
                    }
                    builder.AppendLine("  }");
                    builder.AppendLine("");

                    builder.AppendLine($"  export const {definition.Name} = {{");
                    builder.AppendLine("");
                    builder.AppendLine($"    encode(message: I{definition.Name}): Uint8Array {{");
                    builder.AppendLine(CompileEncode(definition));
                    builder.AppendLine("      },");
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