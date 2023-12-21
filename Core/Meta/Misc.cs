namespace Core.Meta;
using Core.Logging;
using System.Collections.Generic;
using Core.Exceptions;
using System.Text.Json.Serialization;


public record AuxiliaryFile(string Name, string Content) { }
public record GeneratedFile(string Name, string Content, string Generator, AuxiliaryFile? AuxiliaryFile) { }
public record CompilerOutput(List<SpanException> Warnings, List<SpanException> Errors, GeneratedFile[]? Results) { }