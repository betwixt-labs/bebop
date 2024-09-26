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
        if (Definition.Targets == DecoratorTargets.All)
        {
            // Handle special case for 'deprecated' decorator
            if (Target.HasFlag(DecoratorTargets.Struct | DecoratorTargets.Field) &&
                Identifier.Equals("deprecated", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        // Separate container and member flags from Definition.Targets
        var definitionContainerFlags = Definition.Targets & (DecoratorTargets.Enum | DecoratorTargets.Message | DecoratorTargets.Struct | DecoratorTargets.Union | DecoratorTargets.Service);
        var definitionMemberFlags = Definition.Targets & (DecoratorTargets.Field | DecoratorTargets.Method);

        // Separate container and member flags from Target
        var targetContainerFlags = Target & (DecoratorTargets.Enum | DecoratorTargets.Message | DecoratorTargets.Struct | DecoratorTargets.Union | DecoratorTargets.Service);
        var targetMemberFlags = Target & (DecoratorTargets.Field | DecoratorTargets.Method);

        if (definitionContainerFlags != DecoratorTargets.None)
        {
            // Definition specifies container types
            if (definitionMemberFlags != DecoratorTargets.None)
            {
                // Definition specifies container and member types
                // Check if Target includes both the container and member types
                if ((targetContainerFlags & definitionContainerFlags) != DecoratorTargets.None &&
                    (targetMemberFlags & definitionMemberFlags) != DecoratorTargets.None)
                {
                    return true;
                }
            }
            else
            {
                // Definition only specifies container types
                // Check if Target includes the container type
                if ((targetContainerFlags & definitionContainerFlags) != DecoratorTargets.None)
                {
                    return true;
                }
            }
        }
        else if (definitionMemberFlags != DecoratorTargets.None)
        {
            // Definition only specifies member types
            // Check if Target includes the member type
            if ((targetMemberFlags & definitionMemberFlags) != DecoratorTargets.None)
            {
                return true;
            }
        }

        return false;
    }

    public string GetValidTargetsString()
    {
        if (Definition.Targets == DecoratorTargets.All)
        {
            return "all targets";
        }

        var containerTargets = new List<string>();
        var memberTargets = new List<string>();

        bool hasEnum = Definition.Targets.HasFlag(DecoratorTargets.Enum);
        bool hasMessage = Definition.Targets.HasFlag(DecoratorTargets.Message);
        bool hasStruct = Definition.Targets.HasFlag(DecoratorTargets.Struct);
        bool hasUnion = Definition.Targets.HasFlag(DecoratorTargets.Union);
        bool hasService = Definition.Targets.HasFlag(DecoratorTargets.Service);
        bool hasField = Definition.Targets.HasFlag(DecoratorTargets.Field);
        bool hasMethod = Definition.Targets.HasFlag(DecoratorTargets.Method);

        if (hasEnum && !hasField && !hasMethod) containerTargets.Add("enums");
        if (hasMessage && !hasField && !hasMethod) containerTargets.Add("messages");
        if (hasStruct && !hasField && !hasMethod) containerTargets.Add("structs");
        if (hasUnion && !hasField && !hasMethod) containerTargets.Add("unions");
        if (hasService && !hasField && !hasMethod) containerTargets.Add("services");

        if (hasField)
        {
            var fieldContainers = new List<string>();
            if (hasEnum) fieldContainers.Add("enums");
            if (hasMessage) fieldContainers.Add("messages");
            if (hasStruct) fieldContainers.Add("structs");
            if (hasUnion) fieldContainers.Add("unions");

            if (fieldContainers.Count > 0)
            {
                memberTargets.Add($"fields of {FormatTargetList(fieldContainers)}");
            }
            else
            {
                memberTargets.Add("fields");
            }
        }

        if (hasMethod)
        {
            var methodContainers = new List<string>();
            if (hasMessage) methodContainers.Add("messages");
            if (hasService) methodContainers.Add("services");

            if (methodContainers.Count > 0)
            {
                memberTargets.Add($"methods of {FormatTargetList(methodContainers)}");
            }
            else
            {
                memberTargets.Add("methods");
            }
        }

        var allTargets = containerTargets.Concat(memberTargets).ToList();

        if (allTargets.Count == 0)
        {
            return "no targets";
        }
        else
        {
            return FormatTargetList(allTargets);
        }
    }

    private static string FormatTargetList(List<string> targets)
    {
        if (targets.Count == 1)
        {
            return targets[0];
        }
        else
        {
            string lastTarget = targets[targets.Count - 1];
            targets.RemoveAt(targets.Count - 1);
            return string.Join(", ", targets) + " and " + lastTarget;
        }
    }
}