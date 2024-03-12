using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Exceptions;
using Core.Meta;
using Core.Meta.Decorators;
using Core.Parser;
namespace Core.Generators;
public record GeneratorContext(BebopSchema Schema, GeneratorConfig Config)
{
    public override string ToString() => JsonSerializer.Serialize(this, JsonContext.Default.GeneratorContext);
}

internal class GeneratorContextConverter : JsonConverter<GeneratorContext>
{
    public override GeneratorContext Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, GeneratorContext value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteStartObject("definitions");
        var aggregates = value.Schema.Definitions.Where(x => x.Value is StructDefinition or MessageDefinition or EnumDefinition);
        foreach (var (key, def) in aggregates)
        {
            writer.WriteStartObject(key);
            WriteAggregateDefinition(writer, def, value.Schema, options);
            writer.WriteEndObject();
        }
        var unions = value.Schema.Definitions.Where(x => x.Value is UnionDefinition);
        foreach (var (key, def) in unions)
        {
            writer.WriteStartObject(key);
            WriteUnionDefinition(writer, def, value.Schema, options);
            writer.WriteEndObject();
        }
        writer.WriteEndObject();

        writer.WriteStartObject("services");
        var services = value.Schema.Definitions.Values.OfType<ServiceDefinition>();
        foreach (var service in services)
        {
            writer.WriteStartObject(service.Name);
            WriteServiceDefinition(writer, service, value.Schema, options);
            writer.WriteEndObject();
        }
        writer.WriteEndObject();

        var constants = value.Schema.Definitions.Values.OfType<ConstDefinition>();
        writer.WriteStartObject("constants");
        foreach (var constant in constants)
        {
            writer.WriteStartObject(constant.Name);
            WriteConstantDefinition(writer, constant, value.Schema, options);
            writer.WriteEndObject();
        }
        writer.WriteEndObject();

        writer.WriteStartObject("config");
        WriteGeneratorConfig(value.Config, writer);
        writer.WriteEndObject();

        writer.WriteEndObject();
    }

    private static void WriteGeneratorConfig(GeneratorConfig config, Utf8JsonWriter writer)
    {
        writer.WriteString("alias", config.Alias);
        writer.WriteString("outFile", config.OutFile);
        writer.WriteString("namespace", config.Namespace);
        writer.WriteBoolean("emitNotice", config.EmitNotice);
        writer.WriteBoolean("emitBinarySchema", config.EmitBinarySchema);
        writer.WriteString("services", config.Services switch
        {
            TempoServices.None => "none",
            TempoServices.Both => "both",
            TempoServices.Client => "client",
            TempoServices.Server => "server",
            _ => throw new CompilerException("Unknown service type")
        });
        writer.WriteStartObject("options");
        foreach (var (key, val) in config.GetOptions())
        {
            writer.WriteString(key, val);
        }
        writer.WriteEndObject();
    }

    private static void WriteConstantDefinition(Utf8JsonWriter writer, ConstDefinition constant, BebopSchema schema, JsonSerializerOptions options)
    {
        writer.WriteString("kind", GetDefinitionKind(constant));
        if (!string.IsNullOrWhiteSpace(constant.Documentation))
        {
            writer.WriteString("documentation", constant.Documentation);
        }
        writer.WriteString("type", constant.Value.Type.ToTokenString());
        var literal = constant.Value;
        switch (literal)
        {
            case BoolLiteral bl:
                writer.WriteBoolean("value", bl.Value);
                break;
            case IntegerLiteral il:
                writer.WriteString("value", il.Value);
                break;
            case FloatLiteral fl:
                writer.WriteString("value", fl.Value);
                break;
            case StringLiteral sl:
                writer.WriteString("value", sl.Value);
                break;
            case GuidLiteral gl:
                writer.WriteString("value", gl.Value.ToString("N"));
                break;
            default:
                throw new CompilerException($"Unknown literal type {literal.GetType()}");
        }
    }

    private static void WriteServiceDefinition(Utf8JsonWriter writer, ServiceDefinition def, BebopSchema schema, JsonSerializerOptions options)
    {

        writer.WriteString("kind", GetDefinitionKind(def));
        if (!string.IsNullOrWhiteSpace(def.Documentation))
        {
            writer.WriteString("documentation", def.Documentation);
        }
        WriteDecorators(writer, def.Decorators);
        writer.WriteStartObject("methods");
        foreach (var method in def.Methods)
        {
            var methodDefinition = method.Definition;
            writer.WriteStartObject(methodDefinition.Name);
            WriteDecorators(writer, method.Decorators);
            if (!string.IsNullOrWhiteSpace(methodDefinition.Documentation))
            {
                writer.WriteString("documentation", methodDefinition.Documentation);
            }
            writer.WriteString("type", RpcSchema.GetMethodTypeName(methodDefinition.Type));
            writer.WriteString("requestType", methodDefinition.RequestDefinition.ToString());
            writer.WriteString("responseType", methodDefinition.ResponseDefintion.ToString());
            writer.WriteNumber("id", method.Id);
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }

    private static void WriteUnionDefinition(Utf8JsonWriter writer, Definition def, BebopSchema schema, JsonSerializerOptions options)
    {
        if (def is not UnionDefinition ud)
        {
            throw new CompilerException("Expected UnionDefinition");
        }
        writer.WriteString("kind", GetDefinitionKind(def));
        writer.WriteNumber("minimalEncodedSize", ud.MinimalEncodedSize(schema));
        if (!string.IsNullOrWhiteSpace(def.Documentation))
        {
            writer.WriteString("documentation", def.Documentation);
        }
        var branches = def.Dependencies();
        var branchCount = branches.Count();
        if (branchCount < 0 || branchCount > byte.MaxValue)
        {
            throw new CompilerException($"{def.Name} exceeds maximum branches: has {branchCount} branches");
        }
        WriteDecorators(writer, ud.Decorators);
        writer.WriteStartObject("branches");
        for (var i = 0; i < branches.Count(); i++)
        {
            var branch = branches.ElementAt(i);
            // discriminator
            writer.WriteNumber(branch, i + 1);
        }
        writer.WriteEndObject();
    }

    private static void WriteAggregateDefinition(Utf8JsonWriter writer, Definition def, BebopSchema schema, JsonSerializerOptions options)
    {
        writer.WriteString("kind", GetDefinitionKind(def));
        if (!string.IsNullOrWhiteSpace(def.Documentation))
        {
            writer.WriteString("documentation", def.Documentation);
        }
        WriteDecorators(writer, def switch
        {
            StructDefinition sd => sd.Decorators,
            MessageDefinition md => md.Decorators,
            EnumDefinition ed => ed.Decorators,
            _ => null,
        });
        switch (def)
        {
            case StructDefinition sd:
                WriteStruct(sd, schema, writer);
                break;
            case MessageDefinition md:
                WriteMessage(md, schema, writer);
                break;
            case EnumDefinition ed:
                WriteEnum(ed, schema, writer);
                break;
        }
        if (def.Parent is not null)
        {
            writer.WriteString("parent", def.Parent.Name);
        }
    }

    private static void WriteEnum(EnumDefinition ed, BebopSchema schema, Utf8JsonWriter writer)
    {
        writer.WriteBoolean("isBitFlags", ed.IsBitFlags);
        writer.WriteNumber("minimalEncodedSize", ed.ScalarType.MinimalEncodedSize(schema));
        writer.WriteString("baseType", ed.ScalarType.ToTokenString());
        var memberCount = ed.Members.Count;
        if (memberCount > byte.MaxValue)
        {
            throw new CompilerException($"{ed.Name} exceeds maximum members: has {memberCount} members");
        }
        writer.WriteStartObject("members");
        foreach (var member in ed.Members)
        {
            writer.WriteStartObject(member.Name);
            if (!string.IsNullOrWhiteSpace(member.Documentation))
            {
                writer.WriteString("documentation", member.Documentation);
            }
            WriteDecorators(writer, member.Decorators);
            WriteConstant(ed.ScalarType.BaseType, member.ConstantValue, writer);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static void WriteMessage(MessageDefinition md, BebopSchema schema, Utf8JsonWriter writer)
    {
        writer.WriteNumber("minimalEncodedSize", md.MinimalEncodedSize(schema));
        if (md.DiscriminatorInParent is not null)
        {
            writer.WriteNumber("discriminatorInParent", md.DiscriminatorInParent.Value);
        }
        var fieldCount = md.Fields.Count;
        if (fieldCount > byte.MaxValue)
        {
            throw new CompilerException($"{md.Name} exceeds maximum fields: has {fieldCount} fields");
        }
        WriteFields(md, md.Fields, writer);
    }

    private static void WriteStruct(StructDefinition sd, BebopSchema schema, Utf8JsonWriter writer)
    {
        writer.WriteNumber("minimalEncodedSize", sd.MinimalEncodedSize(schema));
        if (sd.DiscriminatorInParent is not null)
        {
            writer.WriteNumber("discriminatorInParent", sd.DiscriminatorInParent.Value);
        }
        writer.WriteBoolean("mutable", sd.IsMutable);
        writer.WriteBoolean("isFixedSize", sd.IsFixedSize(schema));
        var fieldCount = sd.Fields.Count;
        if (fieldCount < 0 || fieldCount > byte.MaxValue)
        {
            throw new CompilerException($"{sd.Name} exceeds maximum fields: has {fieldCount} fields");
        }
        WriteFields(sd, sd.Fields, writer);
    }

    private static void WriteFields(Definition parent, IEnumerable<Field> fields, Utf8JsonWriter writer)
    {
        writer.WriteStartObject("fields");
        foreach (var field in fields)
        {
            WriteField(parent, field, writer);
        }
        writer.WriteEndObject();
    }

    private static void WriteField(Definition parent, Field field, Utf8JsonWriter writer)
    {
        writer.WriteStartObject(field.Name);
        if (!string.IsNullOrWhiteSpace(field.Documentation))
        {
            writer.WriteString("documentation", field.Documentation);
        }
        WriteDecorators(writer, field.Decorators);
        writer.WriteString("type", field.Type.ToTokenString());
        if (field.Type is ArrayType at)
        {
            WriteArray(at, writer);
        }
        else if (field.Type is MapType mt)
        {
            WriteMap(mt, writer);
        }
        if (parent is MessageDefinition)
        {
            writer.WriteNumber("index", (byte)field.ConstantValue);
        }
        writer.WriteEndObject();
    }

    private static void WriteArray(ArrayType arrayType, Utf8JsonWriter writer)
    {
        byte depth = 0;
        var memberType = arrayType.MemberType;
        while (memberType is ArrayType at)
        {
            depth++;
            if (depth > byte.MaxValue)
            {
                throw new CompilerException("Array depth is too large");
            }
            memberType = at.MemberType;
        }
        writer.WriteStartObject("array");
        writer.WriteNumber("depth", depth);
        writer.WriteString("memberType", memberType.ToTokenString());
        if (memberType is MapType mt)
        {
            WriteMap(mt, writer);
        }
        writer.WriteEndObject();
    }

    private static void WriteMap(MapType mapType, Utf8JsonWriter writer)
    {
        writer.WriteStartObject("map");
        writer.WriteString("keyType", mapType.KeyType.ToTokenString());
        writer.WriteString("valueType", mapType.ValueType.ToTokenString());
        if (mapType.ValueType is ArrayType at)
        {
            WriteArray(at, writer);
        }
        else if (mapType.ValueType is MapType mt)
        {
            WriteMap(mt, writer);
        }
        writer.WriteEndObject();
    }

    private static void WriteDecorators(Utf8JsonWriter writer, List<SchemaDecorator>? decorators)
    {
        if (decorators is null || decorators.Count == 0)
        {
            return;
        }
        writer.WriteStartObject("decorators");
        foreach (var decorator in decorators)
        {
            writer.WriteStartObject(decorator.Identifier);
            if (decorator.Arguments.Count > 0)
            {
                writer.WriteStartObject("arguments");
                foreach (var (key, value) in decorator.Arguments)
                {
                    writer.WriteStartObject(key);
                    var parameter = decorator.Definition.Parameters!.Single(x => x.Identifier == key);
                    writer.WriteString("type", parameter.Type.ToTokenString());
                    WriteConstant(parameter.Type, value, writer);
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }

    private static string GetDefinitionKind(Definition def)
    {
        return def switch
        {
            EnumDefinition => "enum",
            StructDefinition => "struct",
            MessageDefinition => "message",
            ServiceDefinition => "service",
            UnionDefinition => "union",
            MethodDefinition => "method",
            ConstDefinition => "const",
            _ => throw new Exception("Unknown definition type")
        };
    }


    private static void WriteConstant(BaseType baseType, object value, Utf8JsonWriter writer)
    {
        if (value is BigInteger bigInt)
        {
            switch (baseType)
            {
                case BaseType.Byte:
                    writer.WriteString("value", ((byte)bigInt).ToString());
                    return;
                case BaseType.UInt16:
                    writer.WriteString("value", ((ushort)bigInt).ToString());
                    return;
                case BaseType.Int16:
                    writer.WriteString("value", ((short)bigInt).ToString());
                    return;
                case BaseType.UInt32:
                    writer.WriteString("value", ((uint)bigInt).ToString());
                    return;
                case BaseType.Int32:
                    writer.WriteString("value", ((int)bigInt).ToString());
                    return;
                case BaseType.UInt64:
                    writer.WriteString("value", ((ulong)bigInt).ToString());
                    return;
            }
        }
        else if (value is string v && baseType.IsNumber())
        {
            switch (baseType)
            {
                case BaseType.Byte:
                    writer.WriteString("value", byte.Parse(v).ToString());
                    return;
                case BaseType.UInt16:
                    writer.WriteString("value", ushort.Parse(v).ToString());
                    return;
                case BaseType.Int16:
                    writer.WriteString("value", short.Parse(v).ToString());
                    return;
                case BaseType.UInt32:
                    writer.WriteString("value", uint.Parse(v).ToString());
                    return;
                case BaseType.Int32:
                    writer.WriteString("value", int.Parse(v).ToString());
                    return;
                case BaseType.UInt64:
                    writer.WriteString("value", ulong.Parse(v).ToString());
                    return;
                case BaseType.Int64:
                    writer.WriteString("value", long.Parse(v).ToString());
                    return;
                case BaseType.Float32:
                    writer.WriteString("value", float.Parse(v).ToString());
                    return;
                case BaseType.Float64:
                    writer.WriteString("value", double.Parse(v).ToString());
                    return;
            }
        }
        else if (value is string s && baseType == BaseType.Guid)
        {
            writer.WriteString("value", Guid.Parse(s).ToString("N"));
            return;
        }
        else if (value is string b && baseType == BaseType.Bool)
        {
            writer.WriteString("value", bool.Parse(b).ToString().ToLowerInvariant());
            return;
        }
        else if (value is string ss && baseType == BaseType.String)
        {
            writer.WriteString("value", ss);
            return;
        }
        throw new CompilerException($"Unknown base type {baseType}");
    }
}