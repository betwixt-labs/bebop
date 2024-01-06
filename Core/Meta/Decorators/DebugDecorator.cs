namespace Core.Meta.Decorators;

public sealed record DebugDecorator() :
DecoratorDefinition("debug", "For debugging the parser", DecoratorTargets.All, false, new DecoratorParameter[]
    {
        new("astring", "The a string value", BaseType.String, true, null, null),
        new("anumber", "The b uint32 value", BaseType.UInt32, true, null, null),
        new("aboolean", "The c bool value", BaseType.Bool, false, "false", null),
        new("anumber2", "The d uint32 value", BaseType.UInt32, false, "0", null),
    })
{
    public override bool TryValidate(out string reason, SchemaDecorator? schemaDecorator = null)
    {
        if (schemaDecorator is null)
        {
            reason = "Schema decorator is null.";
            return false;
        }
        reason = string.Empty;
        return true;
    }
}