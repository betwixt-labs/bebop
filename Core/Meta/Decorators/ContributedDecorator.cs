namespace Core.Meta.Decorators;

public sealed record ContributedDecorator : DecoratorDefinition
{
    public ContributedDecorator(string Identifier, string Description, DecoratorTargets Targets, bool AllowMultiple, DecoratorParameter[]? Parameters = null)
    : base(Identifier, Description, Targets, AllowMultiple, Parameters)
    {

    }

    public override bool TryValidate(out string reason, SchemaDecorator? schemaDecorator = null)
    {
        if (schemaDecorator is null)
        {
            reason = "Schema decorator is null.";
            return false;
        }
        if (Parameters is not null)
        {
            foreach (var parameter in Parameters)
            {
                if (schemaDecorator.Arguments.TryGetValue(parameter.Identifier, out var argumentValue))
                {
                    if (parameter.Validator is not null)
                    {
                        if (!parameter.Validator.Pattern.IsMatch(argumentValue))
                        {
                            reason = parameter.Validator.Reason;
                            return false;
                        }
                    }
                }
            }
        }
        reason = string.Empty;
        return true;
    }
}