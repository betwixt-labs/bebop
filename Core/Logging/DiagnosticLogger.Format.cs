using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Exceptions;
using Core.Meta;
using Core.Meta.Extensions;
using System.Text.Json.Serialization.Metadata;
using Spectre.Console;

namespace Core.Logging;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    AllowTrailingCommas = true,
    UseStringEnumConverter = true,
    DefaultBufferSize = 10)]
[JsonSerializable(typeof(LogFormatter))]
[JsonSerializable(typeof(CompilerOutput))]
[JsonSerializable(typeof(GeneratedFile))]
[JsonSerializable(typeof(AuxiliaryFile))]
public partial class ConfigContext : JsonSerializerContext { }

public partial class DiagnosticLogger
{
    private string FormatDiagnostic(Diagnostic diagnostic)
    {
        var span = diagnostic.Span;
        var message = diagnostic.Message;
        if (diagnostic.Severity == Severity.Warning)
        {
            message += $" (To disable this warning, run bebopc with `--no-warn {diagnostic.ErrorCode}`)";
        }
        switch (_formatter)
        {
            case LogFormatter.MSBuild:
                var where = span == null ? ReservedWords.CompilerName : $"{span?.FileName}({span?.StartColonString(',')})";
                return $"{where} : {diagnostic.Severity.ToString().ToLowerInvariant()} BOP{diagnostic.ErrorCode}: {message}";
            case LogFormatter.Structured:
                where = span == null ? "" : $"Issue located in '{span?.FileName}' at {span?.StartColonString()}: ";
                return $"[{DateTime.Now}][Compiler][{diagnostic.Severity}] {where}{message}";
            case LogFormatter.JSON:
                var options = new JsonSerializerOptions
                {
                    TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault
            ? new DefaultJsonTypeInfoResolver()
            : ConfigContext.Default,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase), new SpanExceptionConverter(), new ExceptionConverter() },
                    
                };
                return JsonSerializer.Serialize(diagnostic, options);
            case LogFormatter.Enhanced:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private string FormatCompilerOutput(CompilerOutput output)
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault
            ? new DefaultJsonTypeInfoResolver()
            : ConfigContext.Default,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            Converters = {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                 new SpanExceptionConverter(),
        new ExceptionConverter()
                }
        };



        return JsonSerializer.Serialize(output, options);
    }

    class SpanExceptionConverter : JsonConverter<SpanException>
    {
        public override SpanException Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, SpanException value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteStartObject("span");
            writer.WriteString("fileName", value.Span.FileName);
            writer.WriteNumber("startLine", value.Span.StartLine);
            writer.WriteNumber("endLine", value.Span.EndLine);
            writer.WriteNumber("startColumn", value.Span.StartColumn);
            writer.WriteNumber("endColumn", value.Span.EndColumn);
            writer.WriteNumber("lines", value.Span.Lines);
            writer.WriteEndObject();
            writer.WriteNumber("errorCode", value.ErrorCode);
            writer.WriteString("severity", value.Severity.ToString().ToCamelCase());
            writer.WriteString("message", value.Message);
            writer.WriteEndObject();
        }
    }

    class ExceptionConverter : JsonConverter<Exception>
    {
        public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            if (value is CompilerException compilerException)
            {
                writer.WriteNumber("errorCode", compilerException.ErrorCode);
            }
            writer.WriteString("message", value.Message);
            writer.WriteEndObject();
        }
    }
}