using System;

namespace Mnema.Common.StringFormatter;

public class DateSpecValidator: ISpecValidator
{
    public string? Validate(string? spec)
    {
        if (string.IsNullOrEmpty(spec))
            return null;

        try
        {
            _ = DateTime.Now.ToString(spec);
            return null;
        }
        catch (FormatException)
        {
            return "invalid date format string";
        }
    }
}
