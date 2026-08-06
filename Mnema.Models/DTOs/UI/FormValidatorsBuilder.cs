using Mnema.Common;

namespace Mnema.Models.DTOs.UI;

public sealed class FormValidatorsBuilder : Builder<GenericBag<object>>
{
    private readonly GenericBag<object> _validators = new();

    public FormValidatorsBuilder WithMinLength(int minLength)
    {
        _validators.SetValue("minLength", minLength);
        return this;
    }

    public FormValidatorsBuilder WithMaxLength(int maxLength)
    {
        _validators.SetValue("maxLength", maxLength);
        return this;
    }

    public FormValidatorsBuilder WithRequired()
    {
        _validators.SetValue("required");
        return this;
    }

    public FormValidatorsBuilder WithMin(int min)
    {
        _validators.SetValue("min", min);
        return this;
    }

    public FormValidatorsBuilder WithMax(int max)
    {
        _validators.SetValue("max", max);
        return this;
    }

    public FormValidatorsBuilder WithPattern(string pattern)
    {
        _validators.SetValue("pattern", pattern);
        return this;
    }

    public FormValidatorsBuilder WithStartsWith(string prefix)
    {
        _validators.SetValue("startsWith", prefix);
        return this;
    }

    public FormValidatorsBuilder WithIsUrl()
    {
        _validators.SetValue("isUrl");
        return this;
    }

    public FormValidatorsBuilder WithServerSideValidation(string urlPath)
    {
        _validators.SetValue("serverSideValidation", urlPath.TrimStart('/'));
        return this;
    }

    public override GenericBag<object> Build()
    {
        return _validators;
    }

    public static GenericBag<object> Required => new FormValidatorsBuilder().WithRequired().Build();
}
