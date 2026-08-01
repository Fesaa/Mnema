namespace Mnema.Models.DTOs.UI;

/// <summary>
/// </summary>
/// <param name="Key">Key to be used for translation</param>
/// <param name="Value">The value to be sent back</param>
public sealed record SelectOption<T>(string Key, T Value)
{
    public bool Default { get; set; }

    public static SelectOption<string> FromString(string value)
    {
        return new SelectOption<string>(value, value);
    }

    public static SelectOption<T> DefaultOption(string key, T value)
    {
        return new SelectOption<T>(key, value)
        {
            Default = true
        };
    }

    public static SelectOption<T> Option(string key, T value)
    {
        return new SelectOption<T>(key, value)
        {
            Default = false
        };
    }
}
