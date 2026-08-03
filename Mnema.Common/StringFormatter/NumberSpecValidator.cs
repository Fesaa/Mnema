namespace Mnema.Common.StringFormatter;

public class NumberSpecValidator(bool allowZero = false): ISpecValidator
{
    private const string ErrorMessage = "expected format '#<padding width>', e.g. '#3'";

    public string? Validate(string? spec)
    {
        if (string.IsNullOrEmpty(spec))
            return null;

        if (!spec.StartsWith('#'))
            return ErrorMessage;

        if (spec.Length == 1)
            return ErrorMessage;

        if (!int.TryParse(spec[1..], out var number))
            return ErrorMessage;

        if (number < 1 || (number == 1 && !allowZero))
            return ErrorMessage;

        return null;
    }
}
