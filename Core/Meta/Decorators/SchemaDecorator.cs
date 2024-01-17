using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using Core.Lexer.Tokenization.Models;

namespace Core.Meta.Decorators;

public sealed record SchemaDecorator(string Identifier, DecoratorTargets Target, Span Span, Dictionary<string, string> Arguments, DecoratorDefinition Definition)
{

    public bool TryGetValue(string parameter, [NotNullWhen(true)] out string? value)
    {
        return Arguments.TryGetValue(parameter, out value);
    }

    public bool IsUsableOn()
    {
        if (Definition.Targets is DecoratorTargets.All)
        {
            if (Target.HasFlag(DecoratorTargets.Struct | DecoratorTargets.Field) && Identifier.Equals("deprecated", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        if (Target == Definition.Targets)
        {
            return true;
        }
        return Definition.Targets.HasFlag(Target);
    }
}