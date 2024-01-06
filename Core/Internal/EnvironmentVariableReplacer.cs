using System;
using System.Collections.Generic;
using System.Security;
using System.Text.RegularExpressions;
using Core.Exceptions;
using Core.Lexer.Tokenization.Models;
namespace Core.Internal;
internal static partial class EnvironmentVariableHelper
{

    private static readonly Regex _templateRegex = TemplateRegex();

    public static string Replace(string input, List<SpanException> errors, Span span)
    {
        return _templateRegex.Replace(input, match =>
        {
            string varName = match.Groups[1].Value;
            string? envVar = null;
            try
            {
                envVar = Get(varName);
                if (string.IsNullOrEmpty(envVar))
                {
                    errors.Add(new EnvironmentVariableNotFoundException(span, $"String substitution failed: environment variable '{varName}' was not found."));
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

    public static string Get(string variableName) => Environment.GetEnvironmentVariable(variableName)?.Trim() ?? string.Empty;

    [GeneratedRegex(@"\$\{([^\}]+)\}")]
    private static partial Regex TemplateRegex();
}