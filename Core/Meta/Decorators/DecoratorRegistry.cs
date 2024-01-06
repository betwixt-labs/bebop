using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Core.Meta.Decorators;

public class DecoratorRegistry
{
    private readonly FrozenDictionary<string, DecoratorDefinition> _decorators;

    public DecoratorRegistry(Dictionary<string, DecoratorDefinition> decorators)
    {
        _decorators = FrozenDictionary.ToFrozenDictionary(decorators);
    }
    public bool Contains(string identifier)
    {
        return _decorators.ContainsKey(identifier);
    }

    public bool TryGet(string identifier, [NotNullWhen(true)] out DecoratorDefinition? decorator)
    {
        return _decorators.TryGetValue(identifier, out decorator);
    }
}
