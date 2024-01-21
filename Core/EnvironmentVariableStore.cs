using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;
using Core.Exceptions;
using Core.Lexer.Tokenization.Models;
namespace Core;
public sealed partial class EnvironmentVariableStore
{
    private readonly FrozenDictionary<string, string> _devVariables;

    public EnvironmentVariableStore(string workingDirectory)
    {
        var devEnvFilePath = Path.Combine(workingDirectory, ".dev.vars");
        if (File.Exists(devEnvFilePath))
        {
            _devVariables = File.ReadAllLines(devEnvFilePath)
                .Select(line => line.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1])
                .ToFrozenDictionary();
        }
        else
        {
            _devVariables = FrozenDictionary<string, string>.Empty;
        }
    }
    public int DevVarsCount => _devVariables.Count;
    public IEnumerable<string> DevVarNames => _devVariables.Keys;

    public string Replace(string input, List<SpanException> errors, Span span)
    {
        return TemplateRegex().Replace(input, match =>
        {
            string varName = match.Groups[1].Value;
            string? envVar = null;
            try
            {
                envVar = Get(varName);
                if (string.IsNullOrEmpty(envVar))
                {
                    errors.Add(new EnvironmentVariableNotFoundException(span, $"String substitution failed: environment variable '{varName}' was not found."));
                    return string.Empty;
                }
                return envVar.Trim();
            }
            catch (SecurityException)
            {
                errors.Add(new EnvironmentVariableNotFoundException(span, $"String substitution failed: environment variable '{varName}' was not found."));
                return string.Empty;
            }
        });
    }

    public string? Get(string variableName)
    {
        if (_devVariables.TryGetValue(variableName, out var value))
        {
            return value.Trim();
        }
        return Environment.GetEnvironmentVariable(variableName)?.Trim();
    }

    [GeneratedRegex(@"\$\{([^\}]+)\}")]
    private static partial Regex TemplateRegex();
}