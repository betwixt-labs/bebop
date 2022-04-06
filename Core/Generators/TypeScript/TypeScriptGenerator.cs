using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Core.Meta;
using Core.Meta.Extensions;

namespace Core.Generators.TypeScript
{
    public class TypeScriptGenerator : BaseGenerator
    {
        const int indentStep = 2;

        // TODO: should we get this list from the SchemaRepo or something to prevent sync issues?
        private static readonly ImmutableHashSet<string> _rpcDefinitionsToIgnore =
            new[]
            {
                "RpcDatagram", "RpcRequestHeader", "RpcResponseHeader", "RpcServiceNameReturn",
                "RpcServiceNameArgs", "RpcRequestDatagram", "RpcResponseOk", "RpcResponseErr",
                "RpcResponseCallNotSupported", "RpcResponseUnknownCall", "RpcResponseInvalidSignature",
                "RpcDecodeError"
            }.ToImmutableHashSet();

        public TypeScriptGenerator(BebopSchema schema) : base(schema) { }

        private static string FormatDocumentation(string documentation, string deprecationReason, int spaces)
        {
            var builder = new IndentedStringBuilder();
            builder.Indent(spaces);
            builder.AppendLine("/**");
            builder.Indent(1);
            foreach (var line in documentation.GetLines())
            {
                builder.AppendLine($"* {line}");
            }

            if (!string.IsNullOrWhiteSpace(deprecationReason))
            {
                builder.AppendLine($"* @deprecated {deprecationReason}");
            }

            builder.AppendLine("*/");
            return builder.ToString();
        }

        private static string FormatDeprecationDoc(string deprecationReason, int spaces)
        {
            var builder = new IndentedStringBuilder();
            builder.Indent(spaces);
            builder.AppendLine("/**");
            builder.Indent(1);
            builder.AppendLine($"* @deprecated {deprecationReason}");
            builder.AppendLine("*/");
            return builder.ToString();
        }

        /// <summary>
        /// Generate the body of the <c>encode</c> function for the given <see cref="Definition"/>.
        /// </summary>
        /// <param name="definition">The definition to generate code for.</param>
        /// <returns>The generated TypeScript <c>encode</c> function body.</returns>
        public string CompileEncode(Definition definition)
        {
            return definition switch
            {
                MessageDefinition d => CompileEncodeMessage(d),
                StructDefinition d => CompileEncodeStruct(d),
                UnionDefinition d => CompileEncodeUnion(d),
                _ => throw new InvalidOperationException($"invalid CompileEncode value: {definition}"),
            };
        }

        private string CompileEncodeMessage(MessageDefinition definition)
        {
            var builder = new IndentedStringBuilder(6);
            builder.AppendLine($"const pos = view.reserveMessageLength();");
            builder.AppendLine($"const start = view.length;");
            foreach (var field in definition.Fields)
            {
                if (field.DeprecatedAttribute != null)
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

        private string CompileEncodeStruct(StructDefinition definition)
        {
            var builder = new IndentedStringBuilder(6);
            foreach (var field in definition.Fields)
            {
                builder.AppendLine(CompileEncodeField(field.Type, $"message.{field.Name.ToCamelCase()}"));
            }

            return builder.ToString();
        }

        private string CompileEncodeUnion(UnionDefinition definition)
        {
            var builder = new IndentedStringBuilder(6);
            builder.AppendLine($"const pos = view.reserveMessageLength();");
            builder.AppendLine($"const start = view.length + 1;");
            builder.AppendLine($"view.writeByte(message.discriminator);");
            builder.AppendLine($"switch (message.discriminator) {{");
            foreach (var branch in definition.Branches)
            {
                builder.AppendLine($"  case {branch.Discriminator}:");
                builder.AppendLine($"    {branch.Definition.Name}.encodeInto(message.value, view);");
                builder.AppendLine($"    break;");
            }

            builder.AppendLine("}");
            builder.AppendLine("const end = view.length;");
            builder.AppendLine("view.fillMessageLength(pos, end - start);");
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
                    $"{tab}const length{depth} = {target}.length;" + nl +
                    $"{tab}view.writeUint32(length{depth});" + nl +
                    $"{tab}for (let {i} = 0; {i} < length{depth}; {i}++) {{" + nl +
                    $"{tab}{tab}{CompileEncodeField(at.MemberType, $"{target}[{i}]", depth + 1, indentDepth + 2)}" +
                    nl +
                    $"{tab}}}" + nl +
                    $"}}",
                MapType mt =>
                    $"view.writeUint32({target}.size);" + nl +
                    $"for (const [k{depth}, v{depth}] of {target}) {{" + nl +
                    $"{tab}{CompileEncodeField(mt.KeyType, $"k{depth}", depth + 1, indentDepth + 1)}" + nl +
                    $"{tab}{CompileEncodeField(mt.ValueType, $"v{depth}", depth + 1, indentDepth + 1)}" + nl +
                    $"}}",
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
                    BaseType.Date => $"view.writeDate({target});",
                    _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                },
                DefinedType dt when Schema.Definitions[dt.Name] is EnumDefinition ed =>
                    CompileEncodeField(ed.ScalarType, target, depth, indentDepth),
                DefinedType dt => $"{dt.Name}.encodeInto({target}, view)",
                _ => throw new InvalidOperationException($"CompileEncodeField: {type}")
            };
        }

        /// <summary>
        /// Generate the body of the <c>decode</c> function for the given <see cref="FieldsDefinition"/>.
        /// </summary>
        /// <param name="definition">The definition to generate code for.</param>
        /// <returns>The generated TypeScript <c>decode</c> function body.</returns>
        public string CompileDecode(Definition definition)
        {
            return definition switch
            {
                MessageDefinition d => CompileDecodeMessage(d),
                StructDefinition d => CompileDecodeStruct(d),
                UnionDefinition d => CompileDecodeUnion(d),
                _ => throw new InvalidOperationException($"invalid CompileDecode value: {definition}"),
            };
        }

        /// <summary>
        /// Generate the body of the <c>decode</c> function for the given <see cref=MessageDefinition"/>,
        /// </summary>
        /// <param name="definition">The message definition to generate code for.</param>
        /// <returns>The generated TypeScript <c>decode</c> function body.</returns>
        private string CompileDecodeMessage(MessageDefinition definition)
        {
            var builder = new IndentedStringBuilder(4);
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
                builder.AppendLine($"    {CompileDecodeField(field.Type, $"message.{field.Name.ToCamelCase()}")}");
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

        private string CompileDecodeStruct(StructDefinition definition)
        {
            var builder = new IndentedStringBuilder(4);
            int i = 0;
            foreach (var field in definition.Fields)
            {
                builder.AppendLine($"let field{i}: {TypeName(field.Type)};");
                builder.AppendLine(CompileDecodeField(field.Type, $"field{i}"));
                i++;
            }

            builder.AppendLine($"let message: I{definition.Name} = {{");
            i = 0;
            foreach (var field in definition.Fields)
            {
                builder.AppendLine($"  {field.Name.ToCamelCase()}: field{i},");
                i++;
            }

            builder.AppendLine("};");
            builder.AppendLine("return message;");
            return builder.ToString();
        }

        private string CompileDecodeUnion(UnionDefinition definition)
        {
            var builder = new IndentedStringBuilder(4);
            builder.AppendLine("const length = view.readMessageLength();");
            builder.AppendLine("const end = view.index + 1 + length;");
            builder.AppendLine("switch (view.readByte()) {");
            foreach (var branch in definition.Branches)
            {
                builder.AppendLine($"  case {branch.Discriminator}:");
                builder.AppendLine(
                    $"    return {{ discriminator: {branch.Discriminator}, value: {branch.Definition.Name}.readFrom(view) }};");
            }

            builder.AppendLine("  default:");
            builder.AppendLine("    view.index = end;");
            builder.AppendLine(
                $"    throw new BebopRuntimeError(\"Unrecognized discriminator while decoding {definition.Name}\");");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private void CompileService(IndentedStringBuilder bldr, ServiceDefinition sd)
        {
            var serviceIdent = sd.Name.ToPascalCase();
            bldr.CodeBlock($"export abstract class {serviceIdent}HandlersDef implements $B.ServiceHandlers", 2, () =>
                {
                    bldr.AppendLine($"readonly $name = \"{serviceIdent}\";")
                        .AppendLine();

                    foreach (var branch in sd.Branches.OrderBy(b => b.Discriminator))
                    {
                        var fn = branch.Definition;
                        var fnIdent = fn.Name.ToCamelCase();
                        var isVoid = fn.ReturnStruct.Fields.Count <= 0;

                        var args = string.Join(", ",
                            (new[] { "deadline: $B.Deadline" }).Concat(
                                fn.ArgumentStruct.Fields.Select(a => $"{a.Name}: {TypeName(a.Type)}")));
                        var ret = isVoid ? "void" : TypeName(fn.ReturnStruct.Fields.First().Type);

                        if (fn.Documentation != "" || fn.Attributes is not null)
                        {
                            var docs = FormatDocumentation(fn.Documentation, fn.Attributes?.Value ?? string.Empty, 0);
                            bldr.AppendLine(docs);
                        }

                        bldr.AppendLine(fnIdent == "serviceName"
                                ? $"async {fnIdent}({args}): Promise<{ret}> {{ return this.$name; }}"
                                : $"async {fnIdent}({args}): Promise<{ret}> {{ throw new $B.LocalRpcError({{discriminator: $B.LocalRpcErrorVariants.NotSupported}}); }}")
                            .AppendLine();
                    }

                    bldr.AppendLine("/** @ignore this should only be called by generated code. */")
                        .CodeBlock(
                            "async $recvCall(datagram: $B.IDatagram, rawHandle: $B.RequestHandle): Promise<void>", 2,
                            () =>
                            {
                                bldr.AppendLine("if (datagram.discriminator != $B.RpcRequestDatagram.discriminator)")
                                    .Append("  throw new BebopRuntimeError")
                                    .AppendEnd("(\"`$recvCall` Should only ever be provided with Requests.\")")
                                    .AppendLine("const { header, opcode, data } = datagram.value;")
                                    // since case statements overlap on var names
                                    .AppendLine("let response: any, args: any, handle: $B.TypedRequestHandle<any>;")
                                    .CodeBlock("switch (opcode)", 2, () =>
                                    {
                                        foreach (var branch in sd.Branches.OrderBy(b => b.Discriminator))
                                        {
                                            var opcode = branch.Discriminator;
                                            var fn = branch.Definition;
                                            var fnIdent = fn.Name.ToCamelCase();
                                            var sig = fn.Signature.Name;
                                            var hasArgs = fn.ArgumentStruct.Fields.Count > 0;
                                            var isVoid = fn.ReturnStruct.Fields.Count <= 0;
                                            var catchSendErr =
                                                $".catch((err: unknown) => $B.handleRespondError(err, this.$name, \"{fnIdent}\", header.id))";
                                            bldr.CodeBlock($"case {opcode}", 2, () =>
                                            {
                                                bldr.AppendLine(
                                                        $"handle = new $B.TypedRequestHandle({fn.ReturnStruct.Name}, rawHandle);")
                                                    .CodeBlock($"if (header.signature != {sig})", 2, () =>
                                                    {
                                                        bldr.AppendLine($"return handle.sendInvalidSigResponse({sig})")
                                                            .AppendLine($"  {catchSendErr};");
                                                    });

                                                if (hasArgs)
                                                {
                                                    bldr.CodeBlock("try", 2, () =>
                                                        {
                                                            bldr.AppendLine(
                                                                $"args = {fn.ArgumentStruct.Name}.decode(data);");
                                                        })
                                                        .CodeBlock("catch (err: unknown)", 2, () =>
                                                        {
                                                            bldr.AppendLine(
                                                                    "return handle.sendDecodeErrorResponse(err as $B.TransportErrorDeserializationError)")
                                                                .AppendLine($"  {catchSendErr};");
                                                        });
                                                }

                                                var args = string.Join(", ",
                                                    (new[] { "$B.CallDetails.deadline(handle)" }).Concat(
                                                        fn.ArgumentStruct.Fields.Select(f =>
                                                            $"args.{f.Name.ToCamelCase()}")));
                                                bldr.CodeBlock("try", 2, () =>
                                                    {
                                                        bldr.AppendLine(isVoid
                                                            ? $"response = await this.{fnIdent}({args});"
                                                            : $"response = {{ value: await this.{fnIdent}({args}) }};");
                                                    })
                                                    .CodeBlock("catch (err)", 2, () =>
                                                    {
                                                        bldr.AppendLine("response = err;");
                                                    })
                                                    .AppendLine("return handle.sendResponse(response)")
                                                    .AppendLine($"  {catchSendErr};");
                                            }, ":", "");
                                        }

                                        bldr.CodeBlock("default", 2, () =>
                                        {
                                            bldr.AppendLine("return rawHandle.sendUnknownCallResponse()")
                                                .AppendLine(
                                                    $"  .catch((err: unknown) => $B.handleRespondError(err, this.$name, \"UNKNOWN\", header.id));");
                                        }, ":", "");
                                    });
                            });
                })
                .AppendLine()
                .CodeBlock($"export class {serviceIdent}Requests implements $B.ServiceRequests", 2, () =>
                {
                    bldr.AppendLine($"readonly $name = \"{serviceIdent}\";")
                        .AppendLine("readonly $ctx: $B.RouterContext;")
                        .AppendLine()
                        .CodeBlock("constructor(ctx: $B.RouterContext)", 2, () =>
                        {
                            bldr.AppendLine("this.$ctx = ctx;");
                        });

                    foreach (var branch in sd.Branches.OrderBy(b => b.Discriminator))
                    {
                        var opcode = branch.Discriminator;
                        var fn = branch.Definition;
                        var fnIdent = fn.Name.ToCamelCase();
                        var fnArgs =
                            string.Join(", ",
                                fn.ArgumentStruct.Fields.Select(f => $"{f.Name.ToCamelCase()}: {TypeName(f.Type)}")
                                    .Concat(new[] { "timeoutSec?: number" }));
                        var hasRet = fn.ReturnStruct.Fields.Count > 0;
                        var retType = hasRet
                            ? TypeName(fn.ReturnStruct.Fields.First().Type)
                            : "void";
                        bldr.AppendLine();

                        if (fn.Documentation != "" || fn.Attributes is not null)
                        {
                            var docs = FormatDocumentation(fn.Documentation, fn.Attributes?.Value ?? string.Empty, 0);
                            bldr.AppendLine(docs);
                        }

                        bldr.CodeBlock($"async {fnIdent}({fnArgs}): Promise<{retType}>", 2, () =>
                        {
                            if (hasRet) bldr.Append("return (");
                            bldr.CodeBlock("await this.$ctx.request", 2, () =>
                            {
                                var argNameList = string.Join(", ",
                                    fn.ArgumentStruct.Fields.Select(f => f.Name.ToCamelCase()));
                                bldr.AppendLine($"{fn.ArgumentStruct.Name},")
                                    .AppendLine($"{fn.ReturnStruct.Name},")
                                    .AppendLine($"{opcode},")
                                    .AppendLine("timeoutSec,")
                                    .AppendLine($"{fn.Signature.Name},")
                                    .AppendLine($"{{{argNameList}}}");
                            }, "(", hasRet ? ")" : ");");
                            if (hasRet) bldr.AppendLine(").value;");
                        });
                    }
                })
                .AppendLine();
        }

        private string ReadBaseType(BaseType baseType)
        {
            return baseType switch
            {
                BaseType.Bool => "!!view.readByte()",
                BaseType.Byte => "view.readByte()",
                BaseType.UInt32 => "view.readUint32()",
                BaseType.Int32 => "view.readInt32()",
                BaseType.Float32 => "view.readFloat32()",
                BaseType.String => "view.readString()",
                BaseType.Guid => "view.readGuid()",
                BaseType.UInt16 => "view.readUint16()",
                BaseType.Int16 => "view.readInt16()",
                BaseType.UInt64 => "view.readUint64()",
                BaseType.Int64 => "view.readInt64()",
                BaseType.Float64 => "view.readFloat64()",
                BaseType.Date => "view.readDate()",
                _ => throw new ArgumentOutOfRangeException()
            };
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
                    $"{tab}let length{depth} = view.readUint32();" + nl +
                    $"{tab}{target} = new {TypeName(at)}(length{depth});" + nl +
                    $"{tab}for (let {i} = 0; {i} < length{depth}; {i}++) {{" + nl +
                    $"{tab}{tab}let x{depth}: {TypeName(at.MemberType)};" + nl +
                    $"{tab}{tab}{CompileDecodeField(at.MemberType, $"x{depth}", depth + 1)}" + nl +
                    $"{tab}{tab}{target}[{i}] = x{depth};" + nl +
                    $"{tab}}}" + nl +
                    $"}}",
                MapType mt =>
                    $"{{" + nl +
                    $"{tab}let length{depth} = view.readUint32();" + nl +
                    $"{tab}{target} = new {TypeName(mt)}();" + nl +
                    $"{tab}for (let {i} = 0; {i} < length{depth}; {i}++) {{" + nl +
                    $"{tab}{tab}let k{depth}: {TypeName(mt.KeyType)};" + nl +
                    $"{tab}{tab}let v{depth}: {TypeName(mt.ValueType)};" + nl +
                    $"{tab}{tab}{CompileDecodeField(mt.KeyType, $"k{depth}", depth + 1)}" + nl +
                    $"{tab}{tab}{CompileDecodeField(mt.ValueType, $"v{depth}", depth + 1)}" + nl +
                    $"{tab}{tab}{target}.set(k{depth}, v{depth});" + nl +
                    $"{tab}}}" + nl +
                    $"}}",
                ScalarType st => $"{target} = {ReadBaseType(st.BaseType)};",
                DefinedType dt when Schema.Definitions[dt.Name] is EnumDefinition ed =>
                    $"{target} = {ReadBaseType(ed.BaseType)} as {dt.Name};",
                DefinedType dt => $"{target} = {dt.Name}.readFrom(view);",
                _ => throw new InvalidOperationException($"CompileDecodeField: {type}")
            };
        }

        /// <summary>
        /// Generate a TypeScript type name for the given <see cref="TypeBase"/>.
        /// </summary>
        /// <param name="type">The field type to generate code for.</param>
        /// <returns>The TypeScript type name.</returns>
        private string TypeName(in TypeBase type)
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
                        BaseType.Date => "Date",
                        _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                    };
                case ArrayType at when at.IsBytes():
                    return "Uint8Array";
                case ArrayType at:
                    return $"Array<{TypeName(at.MemberType)}>";
                case MapType mt:
                    return $"Map<{TypeName(mt.KeyType)}, {TypeName(mt.ValueType)}>";
                case DefinedType dt:
                    var isEnum = Schema.Definitions[dt.Name] is EnumDefinition;
                    return (isEnum ? "" : "I") + dt.Name;
            }

            throw new InvalidOperationException($"GetTypeName: {type}");
        }

        private static string EscapeStringLiteral(string value)
        {
            // TypeScript accepts \u0000 style escape sequences, so we can escape the string JSON-style.
            var options = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            return JsonSerializer.Serialize(value, options);
        }

        private string EmitLiteral(Literal literal)
        {
            return literal switch
            {
                BoolLiteral bl => bl.Value ? "true" : "false",
                IntegerLiteral il when il.Type is ScalarType st && st.Is64Bit => $"BigInt(\"{il.Value}\")",
                IntegerLiteral il => il.Value,
                FloatLiteral fl when fl.Value == "inf" => "Number.POSITIVE_INFINITY",
                FloatLiteral fl when fl.Value == "-inf" => "Number.NEGATIVE_INFINITY",
                FloatLiteral fl when fl.Value == "nan" => "Number.NaN",
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
        public override string Compile(Version? languageVersion, bool writeGeneratedNotice = true)
        {
            var builder = new IndentedStringBuilder();
            if (writeGeneratedNotice)
            {
                builder.AppendLine(GeneratorUtils.GetXmlAutoGeneratedNotice());
            }

            builder.AppendLine("import * as $B from \"bebop\";");
            builder.AppendLine("import { BebopView, BebopRuntimeError } from \"bebop\";");
            builder.AppendLine("");
            if (!string.IsNullOrWhiteSpace(Schema.Namespace))
            {
                builder.AppendLine($"export namespace {Schema.Namespace} {{");
                builder.Indent(2);
            }

            var hasServiceDefinition = Schema.Definitions.Values.Any(d => d is ServiceDefinition);

            foreach (var definition in Schema.Definitions.Values)
            {
                if (hasServiceDefinition && _rpcDefinitionsToIgnore.Contains(definition.Name))
                {
                    // We don't want to generate the RPC types for TS because it is easier to ship them in the runtime
                    // for NotNullWhenAttribute. Also makes it more similar to Rust.
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(definition.Documentation))
                {
                    builder.AppendLine(FormatDocumentation(definition.Documentation, string.Empty, 0));
                }

                if (definition is EnumDefinition ed)
                {
                    var is64Bit = ed.ScalarType.Is64Bit;
                    if (is64Bit)
                    {
                        builder.AppendLine($"export type {ed.Name} = bigint;");
                        builder.AppendLine($"export const {ed.Name} = {{");
                    }
                    else
                    {
                        builder.AppendLine($"export enum {ed.Name} {{");
                    }

                    for (var i = 0; i < ed.Members.Count; i++)
                    {
                        var field = ed.Members.ElementAt(i);
                        var deprecationReason = field.DeprecatedAttribute?.Value ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(field.Documentation))
                        {
                            builder.AppendLine(FormatDocumentation(field.Documentation, deprecationReason, 2));
                        }
                        else if (string.IsNullOrWhiteSpace(field.Documentation) &&
                                 !string.IsNullOrWhiteSpace(deprecationReason))
                        {
                            builder.AppendLine(FormatDeprecationDoc(deprecationReason, 2));
                        }

                        if (is64Bit)
                        {
                            builder.AppendLine($"  {field.Name}: {field.ConstantValue}n,");
                            builder.AppendLine(
                                $"  {EscapeStringLiteral(field.ConstantValue.ToString())}: {EscapeStringLiteral(field.Name)},");
                        }
                        else
                        {
                            builder.AppendLine($"  {field.Name} = {field.ConstantValue},");
                        }
                    }

                    builder.AppendLine(is64Bit ? "};" : "}");
                    builder.AppendLine("");
                }
                else if (definition is RecordDefinition td)
                {
                    if (definition is FieldsDefinition fd)
                    {
                        builder.AppendLine($"export interface I{fd.Name} {{");
                        for (var i = 0; i < fd.Fields.Count; i++)
                        {
                            var field = fd.Fields.ElementAt(i);
                            var type = TypeName(field.Type);
                            var deprecationReason = field.DeprecatedAttribute?.Value ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(field.Documentation))
                            {
                                builder.AppendLine(FormatDocumentation(field.Documentation, deprecationReason, 2));
                            }
                            else if (string.IsNullOrWhiteSpace(field.Documentation) &&
                                     !string.IsNullOrWhiteSpace(deprecationReason))
                            {
                                builder.AppendLine(FormatDeprecationDoc(deprecationReason, 2));
                            }

                            builder.AppendLine(
                                $"  {(fd is StructDefinition { IsReadOnly: true } ? "readonly " : "")}{field.Name.ToCamelCase()}{(fd is MessageDefinition ? "?" : "")}: {type};");
                        }

                        builder.AppendLine("}");
                        builder.AppendLine("");
                    }
                    else if (definition is UnionDefinition ud)
                    {
                        var expression = string.Join("\n  | ",
                            ud.Branches.Select(b =>
                                $"{{ discriminator: {b.Discriminator}, value: I{b.Definition.Name} }}"));
                        if (string.IsNullOrWhiteSpace(expression)) expression = "never";
                        builder.AppendLine($"export type I{ud.Name}\n  = {expression};");
                        builder.AppendLine("");
                    }

                    builder.AppendLine($"export const {td.Name} = {{");
                    if (td.OpcodeAttribute != null)
                    {
                        builder.AppendLine($"  opcode: {td.OpcodeAttribute.Value},");
                    }

                    if (td.DiscriminatorInParent != null)
                    {
                        builder.AppendLine($"  discriminator: {td.DiscriminatorInParent},");
                    }

                    builder.AppendLine($"  encode(message: I{td.Name}): Uint8Array {{");
                    builder.AppendLine("    const view = BebopView.getInstance();");
                    builder.AppendLine("    view.startWriting();");
                    builder.AppendLine("    this.encodeInto(message, view);");
                    builder.AppendLine("    return view.toArray();");
                    builder.AppendLine("  },");
                    builder.AppendLine("");
                    builder.AppendLine($"  encodeInto(message: I{td.Name}, view: BebopView): number {{");
                    builder.AppendLine("    const before = view.length;");
                    builder.AppendLine(CompileEncode(td));
                    builder.AppendLine("    const after = view.length;");
                    builder.AppendLine("    return after - before;");
                    builder.AppendLine("  },");
                    builder.AppendLine("");
                    builder.AppendLine($"  decode(buffer: Uint8Array): I{td.Name} {{");
                    builder.AppendLine($"    const view = BebopView.getInstance();");
                    builder.AppendLine($"    view.startReading(buffer);");
                    builder.AppendLine($"    return this.readFrom(view);");
                    builder.AppendLine($"  }},");
                    builder.AppendLine($"");
                    builder.AppendLine($"  readFrom(view: BebopView): I{td.Name} {{");
                    builder.AppendLine(CompileDecode(td));
                    builder.AppendLine("  },");
                    // need `as const` to use the most strict types such as `1` instead of `number`. Also makes fields readonly.
                    builder.AppendLine("} as const;");
                    builder.AppendLine("");
                }
                else if (definition is ConstDefinition cd)
                {
                    builder.AppendLine($"export const {cd.Name}: {TypeName(cd.Value.Type)} = {EmitLiteral(cd.Value)};");
                    builder.AppendLine("");
                }
                else if (definition is ServiceDefinition sd)
                {
                    CompileService(builder, sd);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported definition {definition}");
                }
            }

            if (!string.IsNullOrWhiteSpace(Schema.Namespace))
            {
                builder.Dedent(2);
                builder.AppendLine("}");
            }

            return builder.ToString();
        }

        public override void WriteAuxiliaryFiles(string outputPath)
        {
            // There is nothing to do here now that BebopView.ts is an npm package.
        }
    }
}