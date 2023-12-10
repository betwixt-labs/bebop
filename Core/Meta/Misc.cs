namespace Core.Meta;
using Core.Logging;
using System.Collections.Generic;
using Core.Exceptions;
using System.Text.Json.Serialization;


public record AuxiliaryFile(string Name, string Contents) { }
public record GeneratedFile(string Name, string Contents, string Generator, AuxiliaryFile? AuxiliaryFile) { }
public record CompilerOutput(List<SpanException> Warnings, List<SpanException> Errors, GeneratedFile? Result) { }