using System;
using System.Linq;
using System.Text;
using Compiler.Meta;
using Compiler.Meta.Extensions;
using Compiler.Meta.Interfaces;

namespace Compiler.Generators.CSharp
{

    

    public class CSharpGenerator : IGenerator
    {

       


        private ISchema _schema;

        public CSharpGenerator()
        {
          
        }

        /// <summary>
        /// Generate a C# type name for the given <see cref="IType"/>.
        /// </summary>
        /// <param name="type">The field type to generate code for.</param>
        /// <returns>The C# type name.</returns>
        private string TypeName(in IType type)
        {
            switch (type)
            {
                case ScalarType st:
                    switch (st.BaseType)
                    {
                        case BaseType.Bool:
                            return "bool";
                        case BaseType.Byte:
                            return "byte";
                        case BaseType.UInt:
                            return "uint";
                        case BaseType.Int:
                            return "int";
                        case BaseType.Float:
                            return "float";
                        case BaseType.String:
                            return "string";
                        case BaseType.Guid:
                            return "Guid";
                    }
                    break;
                case ArrayType at:
                    return $"{TypeName(at.MemberType)}";
                case DefinedType dt:
                    var isEnum = _schema.Definitions[dt.Name].Kind == AggregateKind.Enum;
                    return (isEnum ? string.Empty : "I") + dt.Name;
            }
            throw new InvalidOperationException($"GetTypeName: {type}");
        }

        [System.Obsolete("")]
        public string Compile(ISchema schema)
        {
            _schema = schema;
            var builder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(_schema.Package))
            {
                builder.AppendLine($"namespace {_schema.Package.ToPascalCase()} {{");
            }
            foreach (var definition in _schema.Definitions.Values)
            {
                if (definition.IsEnum())
                {
                    builder.AppendLine($"  public enum {definition.Name} {{");
                    for (var i = 0; i < definition.Fields.Count; i++)
                    {
                        var field = definition.Fields.ElementAt(i);
                        builder.AppendLine(
                            $"      {field.Name} = {field.ConstantValue}{(i + 1 < definition.Fields.Count ? "," : "")}");
                    }
                    builder.AppendLine("  }");
                }

               
                if (definition.IsMessage() || definition.IsStruct())
                {
                    builder.AppendLine($"  public abstract class I{definition.Name.ToPascalCase()} {{");
                    if (definition.IsMessage())
                    {
                        builder.AppendLine($"  #nullable enable");
                    }
                    for (var i = 0; i < definition.Fields.Count; i++)
                    {
                        var field = definition.Fields.ElementAt(i);

                        var type = TypeName(field.Type);
                        if (field.DeprecatedAttribute.HasValue && !string.IsNullOrWhiteSpace(field.DeprecatedAttribute.Value.Message))
                        {
                            builder.AppendLine($"    [System.Obsolete(\"{field.DeprecatedAttribute.Value.Message}\")]");
                        }
                        builder.AppendLine($"    public {type}{(definition.Kind == AggregateKind.Message ? "?" : "")} {field.Name.ToPascalCase()} {{ get; {(definition.IsReadOnly ? "init;" : "set;")} }}");
                    }
                    if (definition.IsMessage())
                    {
                        builder.AppendLine($"  #nullable disable");
                    }
                    builder.AppendLine("  }");
                    builder.AppendLine("");

                    builder.AppendLine($"  public class {definition.Name.ToPascalCase()} : I{definition.Name.ToPascalCase()} {{");
                    builder.AppendLine("");
                    builder.AppendLine(CompileEncodeHelper(definition));
                    builder.AppendLine($"    public static void EncodeInto(I{definition.Name.ToPascalCase()} message, PierogiView view) {{");
                    builder.AppendLine(CompileEncode(definition));
                    builder.AppendLine("    }");
                    builder.AppendLine("");

                    builder.AppendLine($"    public static I{definition.Name.ToPascalCase()} DecodeFrom(PierogiView view) {{");
                    builder.AppendLine(CompileDecode(definition));
                    builder.AppendLine("  }");
                }
            }


            if (!string.IsNullOrWhiteSpace(_schema.Package))
            {
                builder.AppendLine("}");
            }
            builder.AppendLine("");


            return builder.ToString().TrimEnd();
        }

        /// <summary>
        /// Generate the body of the <c>DecodeFrom</c> function for the given <see cref="IDefinition"/>.
        /// </summary>
        /// <param name="definition">The definition to generate code for.</param>
        /// <returns>The generated C# <c>DecodeFrom</c> function body.</returns>
        public string CompileDecode(IDefinition definition)
        {
            return definition.Kind switch
            {
                AggregateKind.Message => CompileDecodeMessage(definition),
                AggregateKind.Struct => CompileDecodeStruct(definition),
                _ => throw new InvalidOperationException($"invalid CompileDecode kind: {definition.Kind} in {definition}"),
            };
        }

        private string CompileDecodeStruct(IDefinition definition)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"      var message = new {definition.Name.ToPascalCase()}() {{");
            foreach (var field in definition.Fields)
            {
                builder.AppendLine($"        {field.Name.ToPascalCase()} = {CompileDecodeField(field.Type)},");
            }
            builder.AppendLine("      };");
            builder.AppendLine("      return message;");
            builder.AppendLine("    }");
            return builder.ToString();
        }

        /// <summary>
        /// Generate the body of the <c>DecodeFrom</c> function for the given <see cref="IDefinition"/>,
        /// given that its "kind" is Message.
        /// </summary>
        /// <param name="definition">The message definition to generate code for.</param>
        /// <returns>The generated C# <c>DecodeFrom</c> function body.</returns>
        private string CompileDecodeMessage(IDefinition definition)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"      var message = new {definition.Name.ToPascalCase()}();");
            builder.AppendLine("      while (true) {");
            builder.AppendLine("        switch (view.ReadUint()) {");
            builder.AppendLine("          case 0:");
            builder.AppendLine("            return message;");
            builder.AppendLine("");
            foreach (var field in definition.Fields)
            {
                builder.AppendLine($"          case {field.ConstantValue}:");
                builder.AppendLine($"            message.{field.Name.ToPascalCase()} = {CompileDecodeField(field.Type)};");
                builder.AppendLine("            break;");
                builder.AppendLine("");
            }
            builder.AppendLine("          default:");
            builder.AppendLine("            throw new System.Exception(\"Attempted to parse invalid message\");");
            builder.AppendLine("        }");
            builder.AppendLine("      }");
            builder.AppendLine("    }");
            return builder.ToString();
        }


        private string CompileDecodeField(IType type)
        {
            switch (type)
            {
                case ArrayType at when at.IsBytes():
                    return "view.ReadBytes()";
                case ArrayType at:
                    return @$"(() => {{
                        let length = view.readUint();
                        var collection = new {TypeName(at)}[length];
                        for (var i = 0; i < length; i++) collection[i] = {CompileDecodeField(at.MemberType)};
                        return collection;
                    }})()";
                case ScalarType st:
                    switch (st.BaseType)
                    {
                        case BaseType.Bool: return "view.ReadByte() != 0";
                        case BaseType.Byte: return "view.ReadByte()";
                        case BaseType.UInt: return "view.ReadUint()";
                        case BaseType.Int: return "view.ReadInt()";
                        case BaseType.Float: return "view.ReadFloat()";
                        case BaseType.String: return "view.ReadString()";
                        case BaseType.Guid: return "view.ReadGuid()";
                    }
                    break;
                case DefinedType dt when _schema.Definitions[dt.Name].Kind == AggregateKind.Enum:
                    return $"view.ReadUint() as {dt.Name}";
                case DefinedType dt:
                    return $"{(string.IsNullOrWhiteSpace(_schema.Package) ? string.Empty : $"{_schema.Package.ToPascalCase()}.")}{dt.Name.ToPascalCase()}.DecodeFrom(view)";
            }
            throw new InvalidOperationException($"CompileDecodeField: {type}");
        }

        /// <summary>
        /// Generates the body of a helper method to decode the given <see cref="IDefinition"/>
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        public string CompileDecodeHelper(IDefinition definition)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"    public static I{definition.Name.ToPascalCase()} Decode(byte[] message) {{");
            builder.AppendLine("        var view = new PierogiView(message);");
            builder.AppendLine("        return DecodeFrom(view);");
            builder.AppendLine("      }");
            return builder.ToString();
        }

        /// <summary>
        /// Generates the body of a helper method to encode the given <see cref="IDefinition"/>
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        public string CompileEncodeHelper(IDefinition definition)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"    public static byte[] Encode(I{definition.Name.ToPascalCase()} message) {{");
            builder.AppendLine("        var view = new PierogiView();");
            builder.AppendLine("        EncodeInto(message, view);");
            builder.AppendLine("        return view.ToArray();");
            builder.AppendLine("      }");
            return builder.ToString();
        }

        /// <summary>
        /// Generate the body of the <c>EncodeTo</c> function for the given <see cref="IDefinition"/>.
        /// </summary>
        /// <param name="definition">The definition to generate code for.</param>
        /// <returns>The generated C# <c>EncodeTo</c> function body.</returns>
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
            var builder = new StringBuilder();
            foreach (var field in definition.Fields)
            {
                if (field.DeprecatedAttribute.HasValue)
                {
                    continue;
                }
                builder.AppendLine("");
                builder.AppendLine($"      if (message.{field.Name.ToPascalCase()} != null) {{");
                builder.AppendLine($"        view.WriteUInt({field.ConstantValue});");
                builder.AppendLine($"        {CompileEncodeField(field.Type, $"message.{field.Name.ToPascalCase()}")}");
                builder.AppendLine($"      }}");
            }
            builder.AppendLine("      view.WriteUInt(0);");
            return builder.ToString();
        }


        private string CompileEncodeStruct(IDefinition definition)
        {
            var builder = new StringBuilder();
            foreach (var field in definition.Fields)
            {
                builder.AppendLine($"      {CompileEncodeField(field.Type, $"message.{field.Name.ToPascalCase()}")}");
            }
            return builder.ToString();
        }


        private string CompileEncodeField(IType type, string target, int depth = 0)
        {
            switch (type)
            {
                case ArrayType at when at.IsBytes():
                    return $"view.WriteBytes({target});";
                case ArrayType at:
                    var indent = new string(' ', (depth + 4) * 2);
                    var i = GeneratorUtils.LoopVariable(depth);
                    return $"var length{depth} = {target}.length;\n"
                        + indent + $"view.WriteUInt(length{depth});\n"
                        + indent + $"for (var {i} = 0; {i} < length{depth}; {i}++) {{\n"
                        + indent + $"    {CompileEncodeField(at.MemberType, $"{target}[{i}]", depth + 1)}\n"
                        + indent + "}";
                case ScalarType st:
                    switch (st.BaseType)
                    {
                        case BaseType.Bool: return $"view.WriteByte({target});";
                        case BaseType.Byte: return $"view.WriteByte({target});";
                        case BaseType.UInt: return $"view.WriteUInt({target});";
                        case BaseType.Int: return $"view.WriteInt({target});";
                        case BaseType.Float: return $"view.WriteFloat({target});";
                        case BaseType.String: return $"view.WriteString({target});";
                        case BaseType.Guid: return $"view.WriteGuid({target});";
                    }
                    break;
                case DefinedType dt when _schema.Definitions[dt.Name].Kind == AggregateKind.Enum:
                    return $"view.WriteEnum({target});";
                case DefinedType dt:
                    return $"{(string.IsNullOrWhiteSpace(_schema.Package) ? string.Empty : $"{_schema.Package.ToPascalCase()}.")}{dt.Name.ToPascalCase()}.EncodeInto({target}, view);";
            }
            throw new InvalidOperationException($"CompileEncodeField: {type}");
        }

        public string OutputFileName(ISchema schema)
        {
            throw new NotImplementedException();
        }

        public void WriteAuxiliaryFiles(string outputPath)
        {
            throw new NotImplementedException();
        }
    }
}
