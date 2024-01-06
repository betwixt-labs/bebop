namespace Core.Meta.Decorators
{

    public sealed record FlagsDecorator() : DecoratorDefinition("flags", "A decorator that marks an enum as a set of bit flags.", DecoratorTargets.Enum, false)
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
}