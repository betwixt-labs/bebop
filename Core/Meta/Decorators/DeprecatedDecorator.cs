using System.Linq;

namespace Core.Meta.Decorators
{

    public sealed record DeprecatedDecorator() : DecoratorDefinition("deprecated", "A decorator that marks types as deprecated. When applied to fields of messages they are skipped over.",
    DecoratorTargets.All,
    false,
    new[]
    {
        new DecoratorParameter("reason", "The reason for the deprecation.", BaseType.String, true)
    })
    {
        public override bool TryValidate(out string reason, SchemaDecorator? schemaDecorator = null)
        {
            if (schemaDecorator is null)
            {
                reason = "Schema decorator is null.";
                return false;
            }
            if (!schemaDecorator.Arguments.TryGetValue("reason", out var value))
            {
                reason = "No value found for 'reason'.";
                return false;

            }
            if (string.IsNullOrWhiteSpace(value))
            {
                reason = "The deprecated reason cannot be an empty strings.";
                return false;
            }
            reason = string.Empty;
            return true;
        }
    }
}