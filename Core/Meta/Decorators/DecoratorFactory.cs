using System;
using System.Collections.Generic;
using Core.Exceptions;

namespace Core.Meta.Decorators;

public class DecoratorFactory
{
    private readonly Dictionary<string, DecoratorDefinition> _decorators;

    public DecoratorFactory()
    {
        _decorators = [];
    }

    public void Register(Func<DecoratorDefinition> mapper)
    {
        var instance = mapper();
        if (instance is null)
        {
            throw new CompilerException("mapper returned null");
        }
        if (_decorators.ContainsKey(instance.Identifier))
        {
            throw new CompilerException($"unable to register '{instance.Identifier}': already registered.");
        }
        _decorators.Add(instance.Identifier, instance);
    }

    public void Register(DecoratorDefinition decorator)
    {
        if (_decorators.ContainsKey(decorator.Identifier))
        {
            throw new CompilerException($"unable to register '{decorator.Identifier}': already registered.");
        }
        _decorators.Add(decorator.Identifier, decorator);
    }

    public void RegisterDefault()
    {
        Register(new FlagsDecorator());
        Register(new OpcodeDecorator());
        Register(new DeprecatedDecorator());
        Register(new DebugDecorator());
    }

    public DecoratorRegistry Build()
    {
        try
        {
            return new DecoratorRegistry(_decorators);
        }
        finally
        {
            _decorators.Clear();
        }
    }
}