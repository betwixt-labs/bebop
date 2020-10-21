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

        public string Compile(ISchema schema)
        {
            _schema = schema;
            var builder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(_schema.Namespace))
            {
                builder.AppendLine($"namespace {_schema.Namespace.ToPascalCase()} {{");
            }
            foreach (var definition in _schema.Definitions.Values)
            {
                if (!string.IsNullOrWhiteSpace(definition.Documentation))
                {
                    builder.AppendLine("  /// <summary>");
                    foreach (var line in definition.Documentation.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None
                    ))
                    {
                        builder.AppendLine($"  /// {line}");
                    }
                    builder.AppendLine("  /// </summary>");
                }
                if (definition.IsEnum())
                {
                    builder.AppendLine($"  public enum {definition.Name} {{");
                    for (var i = 0; i < definition.Fields.Count; i++)
                    {
                        var field = definition.Fields.ElementAt(i);
                        if (!string.IsNullOrWhiteSpace(field.Documentation))
                        {
                            builder.AppendLine("      /// <summary>");
                            foreach (var line in field.Documentation.Split(new[] { "\r\n", "\r", "\n" },
                                StringSplitOptions.None))
                            {
                                builder.AppendLine($"      /// {line}");
                            }
                            builder.AppendLine("      /// </summary>");
                        }
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
                        builder.AppendLine("  #nullable enable");
                    }
                    for (var i = 0; i < definition.Fields.Count; i++)
                    {
                        var field = definition.Fields.ElementAt(i);

                        var type = TypeName(field.Type);
                        if (!string.IsNullOrWhiteSpace(field.Documentation))
                        {
                            builder.AppendLine("    /// <summary>");
                            foreach (var line in field.Documentation.Split(new[] {"\r\n", "\r", "\n"},
                                StringSplitOptions.None))
                            {
                                builder.AppendLine($"    /// {line}");
                            }
                            builder.AppendLine("    /// </summary>");
                        }
                        if (field.DeprecatedAttribute.HasValue &&
                            !string.IsNullOrWhiteSpace(field.DeprecatedAttribute.Value.Message))
                        {
                            builder.AppendLine($"    [System.Obsolete(\"{field.DeprecatedAttribute.Value.Message}\")]");
                        }
                        builder.AppendLine(
                            $"    public {type}{(definition.Kind == AggregateKind.Message ? "?" : "")} {field.Name.ToPascalCase()} {{ get; {(definition.IsReadOnly ? "init;" : "set;")} }}");
                    }
                    if (definition.IsMessage())
                    {
                        builder.AppendLine("  #nullable disable");
                    }
                    builder.AppendLine("  }");
                    builder.AppendLine("");
                    builder.AppendLine("  /// <inheritdoc />");
                    builder.AppendLine(
                        $"  public class {definition.Name.ToPascalCase()} : I{definition.Name.ToPascalCase()} {{");
                    builder.AppendLine("");
                    builder.AppendLine(CompileEncodeHelper(definition));
                    builder.AppendLine(
                        $"    public static void EncodeInto(I{definition.Name.ToPascalCase()} message, PierogiView view) {{");
                    builder.AppendLine(CompileEncode(definition));
                    builder.AppendLine("    }");
                    builder.AppendLine("");

                    builder.AppendLine(
                        $"    public static I{definition.Name.ToPascalCase()} DecodeFrom(PierogiView view) {{");
                    builder.AppendLine(CompileDecode(definition));
                    builder.AppendLine("  }");
                }
            }


            if (!string.IsNullOrWhiteSpace(_schema.Namespace))
            {
                builder.AppendLine("}");
            }
            builder.AppendLine("");


            return builder.ToString().TrimEnd();
        }

        public void WriteAuxiliaryFiles(string outputPath)
        {
           
        }

        /// <summary>
        ///     Generate a C# type name for the given <see cref="IType"/>.
        /// </summary>
        /// <param name="type">The field type to generate code for.</param>
        /// <param name="arraySizeVar">A variable name that will be formatted into the array initializer</param>
        /// <returns>The C# type name.</returns>
        private string TypeName(in IType type , string arraySizeVar = "")
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
                        _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                    };
                case ArrayType at: 
                    return $"{(at.MemberType is ArrayType ? ($"{TypeName(at.MemberType, arraySizeVar)}[]") : $"{TypeName(at.MemberType)}[{arraySizeVar}]")}";
                case DefinedType dt:
                    var isEnum = _schema.Definitions[dt.Name].Kind == AggregateKind.Enum;
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
        ///     Generate the body of the <c>DecodeFrom</c> function for the given <see cref="IDefinition"/>,
        ///     given that its "kind" is Message.
        /// </summary>
        /// <param name="definition">The message definition to generate code for.</param>
        /// <returns>The generated C# <c>DecodeFrom</c> function body.</returns>
        private string CompileDecodeMessage(IDefinition definition)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"      var message = new {definition.Name.ToPascalCase()}();");
            builder.AppendLine("      var length = view.ReadMessageLength();");
            builder.AppendLine("      var end = view.Position + length;");
            builder.AppendLine("      while (true) {");
            builder.AppendLine("        switch (view.ReadByte()) {");
            builder.AppendLine("          case 0:");
            builder.AppendLine("            return message;");
            builder.AppendLine("");
            foreach (var field in definition.Fields)
            {
                builder.AppendLine($"          case {field.ConstantValue}:");
                builder.AppendLine(
                    $"            message.{field.Name.ToPascalCase()} = {CompileDecodeField(field.Type)};");
                builder.AppendLine("            break;");
                builder.AppendLine("");
            }
            builder.AppendLine("          default:");
            builder.AppendLine("            view.Position = end;");
            builder.AppendLine("            return message;");
            builder.AppendLine("        }");
            builder.AppendLine("      }");
            builder.AppendLine("    }");
            return builder.ToString();
        }


        private string CompileDecodeField(IType type)
        {
            return type switch
            {
                ArrayType at when at.IsBytes() => "view.ReadBytes()",
                ArrayType at => @$"new System.Func<{TypeName(at)}>(() =>
                        {{
                        var length = view.ReadUInt();
                        var collection = new {TypeName(at, "length")};
                        for (var i = 0; i < length; i++) collection[i] = {CompileDecodeField(at.MemberType)};
                        return collection;
                    }}).Invoke()",
                ScalarType st => st.BaseType switch
                {
                    BaseType.Bool => "view.ReadByte() != 0",
                    BaseType.Byte => "view.ReadByte()",
                    BaseType.UInt32 => "view.ReadUInt32()",
                    BaseType.Int32 => "view.ReadInt32()",
                    BaseType.Float32 => "view.ReadFloat32()",
                    BaseType.String => "view.ReadString()",
                    BaseType.Guid => "view.ReadGuid()",
                    BaseType.UInt16 => "view.ReadUInt16()",
                    BaseType.Int16 => "view.ReadInt16()",
                    BaseType.UInt64 => "view.ReadUInt64()",
                    BaseType.Int64 => "view.ReadInt64()",
                    BaseType.Float64 => "view.ReadFloat64()",
                    _ => throw new ArgumentOutOfRangeException()
                },
                DefinedType dt when _schema.Definitions[dt.Name].Kind == AggregateKind.Enum =>
                    $"view.ReadUint() as {dt.Name}",
                DefinedType dt =>
                    $"{(string.IsNullOrWhiteSpace(_schema.Namespace) ? string.Empty : $"{_schema.Namespace.ToPascalCase()}.")}{dt.Name.ToPascalCase()}.DecodeFrom(view)",
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
            var builder = new StringBuilder();
            builder.AppendLine($"    public static I{definition.Name.ToPascalCase()} Decode(byte[] message) {{");
            builder.AppendLine("        var view = new PierogiView(message);");
            builder.AppendLine("        return DecodeFrom(view);");
            builder.AppendLine("      }");
            return builder.ToString();
        }

        /// <summary>
        ///     Generates the body of a helper method to encode the given <see cref="IDefinition"/>
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
            var builder = new StringBuilder();
            builder.AppendLine($"      var pos = view.ReserveMessageLength();");
            builder.AppendLine($"      var start = view.Length;");
            foreach (var field in definition.Fields)
            {
                if (field.DeprecatedAttribute.HasValue)
                {
                    continue;
                }
                builder.AppendLine("");
                builder.AppendLine($"      if (message.{field.Name.ToPascalCase()} != null) {{");
                builder.AppendLine($"        view.WriteByte({field.ConstantValue});");
                builder.AppendLine($"        {CompileEncodeField(field.Type, $"message.{field.Name.ToPascalCase()}")}");
                builder.AppendLine("      }");
            }
            builder.AppendLine("      view.WriteByte(0);");
            builder.AppendLine("      var end = view.Length;");
            builder.AppendLine("      view.FillMessageLength(pos, end - start);");
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
            var indent = new string(' ', (depth + 4) * 2);
            var i = GeneratorUtils.LoopVariable(depth);
            return type switch
            {
                ArrayType at when at.IsBytes() => $"view.WriteBytes({target});",
                ArrayType at when at.IsFloat32s() => $"view.WriteFloat32s({target});",
                ArrayType at when at.IsFloat64s() => $"view.WriteFloat64s({target});",
                ArrayType at => $"var length{depth} = {target}.Length;\n" + indent +
                    $"view.WriteUInt32(length{depth});\n" + indent +
                    $"for (var {i} = 0; {i} < length{depth}; {i}++) {{\n" + indent +
                    $"    {CompileEncodeField(at.MemberType, $"{target}[{i}]", depth + 1)}\n" + indent + "}",
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
                    _ => throw new ArgumentOutOfRangeException()
                },
                DefinedType dt when _schema.Definitions[dt.Name].Kind == AggregateKind.Enum =>
                    $"view.WriteEnum({target});",
                DefinedType dt =>
                    $"{(string.IsNullOrWhiteSpace(_schema.Namespace) ? string.Empty : $"{_schema.Namespace.ToPascalCase()}.")}{dt.Name.ToPascalCase()}.EncodeInto({target}, view);",
                _ => throw new InvalidOperationException($"CompileEncodeField: {type}")
            };
        }
    }
}