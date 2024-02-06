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
            _devVariables = ParseDevVars(File.ReadAllText(devEnvFilePath));
        }
        else
        {
            _devVariables = FrozenDictionary<string, string>.Empty;
        }
    }
    private static FrozenDictionary<string, string> ParseDevVars(string fileContent)
    {
        fileContent = fileContent.Replace("\r\n", "\n");
        var result = new Dictionary<string, string>();

        foreach (Match match in LineRegex().Matches(fileContent))
        {
            if (match.Success)
            {
                var key = match.Groups[1].Value;
                var value = match.Groups[2].Value.Trim();

                if (value.StartsWith("'") || value.StartsWith("\"") || value.StartsWith("`"))
                {
                    value = UnquoteRegex().Replace(value, "$2");
                }

                if (value.StartsWith("\""))
                {
                    value = value.Replace("\\n", "\n").Replace("\\r", "\r");
                }

                if (!string.IsNullOrEmpty(key) && !result.ContainsKey(key))
                {
                    result[key] = value;
                }
            }
        }
        return result.ToFrozenDictionary();
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
    [GeneratedRegex(@"\s*([\w.-]+)(?:\s*=\s*?|:\s+?)(\s*'(?:\\'|[^'])*'|\s*""(?:\\""|[^""])*""|\s*`(?:\\`|[^`])*`|[^#\r\n]+)?\s*(?:#.*)?", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex LineRegex();
    [GeneratedRegex(@"^(['""`])([\s\S]*)\1$")]
    private static partial Regex UnquoteRegex();
}