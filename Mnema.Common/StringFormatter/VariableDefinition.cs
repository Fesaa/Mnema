using System;

namespace Mnema.Common.StringFormatter;

public interface IVariableDefinition<in T>
{
    string? Resolve(T context, string? spec);
    string? SpecValidator(string? spec);
}

public interface IStringResolver<in T>
{
    string? Resolve(T context, string? spec);
}

public interface ISpecValidator
{
    string? Validate(string? spec);
}

public class VariableDefinitionBuilder<T>
{
    private IStringResolver<T>? _resolver;
    private ISpecValidator? _specValidator;

    public VariableDefinitionBuilder<T> WithResolver(FormatVariableResolver<T> resolver)
    {
        _resolver = new SimpleResolver<T>(resolver);
        return this;
    }

    public VariableDefinitionBuilder<T> WithSpecValidator(Func<string?, string?>? validator)
    {
        if (validator is not null)
            _specValidator = new SimpleSpecValidator(validator);
        return this;
    }

    public VariableDefinitionBuilder<T> WithResolver(IStringResolver<T> resolver)
    {
        _resolver = resolver;
        return this;
    }

    public VariableDefinitionBuilder<T> WithSpecValidator(ISpecValidator specValidator)
    {
        _specValidator = specValidator;
        return this;
    }

    public IVariableDefinition<T> Build()
    {
        if (_resolver == null) throw new InvalidOperationException("Resolver is not set");

        return new VariableDefinition(_resolver, _specValidator);
    }

    private class VariableDefinition(IStringResolver<T> resolver, ISpecValidator? specValidator): IVariableDefinition<T>
    {
        public string? Resolve(T context, string? spec)
        {
            return resolver.Resolve(context, spec);
        }

        public string? SpecValidator(string? spec)
        {
            return specValidator?.Validate(spec);
        }
    }
}

internal class SimpleResolver<T>(FormatVariableResolver<T> resolver) : IStringResolver<T>
{
    public string? Resolve(T context, string? spec)
    {
        return resolver(context, spec);
    }
}

internal class SimpleSpecValidator(Func<string?, string?> validator) : ISpecValidator
{
    public string? Validate(string? spec)
    {
        return validator(spec);
    }
}
