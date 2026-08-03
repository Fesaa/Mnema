using System;
using System.Collections.Generic;
using System.Text;

namespace Mnema.Common.StringFormatter;

public interface IStringFormatter<in T>
{
    string Apply(string format, T context);
    IReadOnlyList<string> Validate(string format);
    bool IsValid(string format);
}

public delegate string? FormatVariableResolver<in T>(T context, string? formatSpec);

public class StringFormatter<T>: IStringFormatter<T>
{

    private readonly Dictionary<string, IVariableDefinition<T>> _variables = new(StringComparer.OrdinalIgnoreCase);

    public StringFormatter<T> WithVariable(string name, IVariableDefinition<T> variable)
    {
        _variables[name] = variable;
        return this;
    }

    public StringFormatter<T> WithVariable(string name, Func<T, string?> resolve)
        => WithVariable(name, (ctx, _) => resolve(ctx));

    private StringFormatter<T> WithVariable(string name, FormatVariableResolver<T> resolve,
        Func<string?, string?>? specValidator = null)
    {
        return WithVariable(name, new VariableDefinitionBuilder<T>()
            .WithResolver(resolve)
            .WithSpecValidator(specValidator)
            .Build());
    }

    public string Apply(string format, T context) => Render(format, 0, out _, context).Text;

    private (string Text, bool HasValue) Render(string format, int start, out int end, T context)
    {
        var sb = new StringBuilder();
        var hasValue = false;
        var i = start;

        while (i < format.Length && format[i] != ']')
        {
            switch (format[i])
            {
                case '[':
                {
                    var (text, innerHasValue) = Render(format, i + 1, out var innerEnd, context);
                    if (innerEnd >= format.Length)
                        throw new FormatException($"Unmatched '[' in format \"{format}\"");

                    if (innerHasValue)
                    {
                        sb.Append(text);
                        hasValue = true;
                    }

                    i = innerEnd + 1; // skip past ']'
                    break;
                }
                case '{':
                {
                    var close = format.IndexOf('}', i);
                    if (close < 0)
                        throw new FormatException($"Unterminated '{{' in format \"{format}\"");

                    var token = format[(i + 1)..close];
                    var colon = token.IndexOf(':');
                    var name = colon >= 0 ? token[..colon] : token;
                    var spec = colon >= 0 ? token[(colon + 1)..] : null;

                    if (!_variables.TryGetValue(name, out var variable))
                        throw new FormatException($"Unknown variable '{{{name}}}' in format \"{format}\"");

                    var value = variable.Resolve(context, spec);
                    if (!string.IsNullOrEmpty(value))
                    {
                        sb.Append(value);
                        hasValue = true;
                    }

                    i = close + 1;
                    break;
                }
                default:
                    sb.Append(format[i]);
                    i++;
                    break;
            }
        }

        end = i;
        return (sb.ToString(), hasValue);
    }

    /// <summary>
    /// Validates a format string without needing a context instance.
    /// Returns all errors found (unknown variables, unmatched/unterminated
    /// brackets or braces). Empty list means the format is valid.
    /// </summary>
    public IReadOnlyList<string> Validate(string format)
    {
        var errors = new List<string>();
        var bracketDepth = 0;
        var i = 0;

        while (i < format.Length)
        {
            switch (format[i])
            {
                case '[':
                    bracketDepth++;
                    i++;
                    break;

                case ']':
                    if (bracketDepth == 0)
                        errors.Add($"Unmatched ']' at position {i}");
                    else
                        bracketDepth--;
                    i++;
                    break;

                case '{':
                {
                    var close = format.IndexOf('}', i);
                    if (close < 0)
                    {
                        errors.Add($"Unterminated '{{' at position {i}");
                        i = format.Length; // nothing more to usefully parse
                        break;
                    }

                    var token = format[(i + 1)..close];
                    var colon = token.IndexOf(':');
                    var name = colon >= 0 ? token[..colon] : token;

                    if (name.Length == 0)
                    {
                        errors.Add($"Empty variable name at position {i}");
                    }
                    else if (!_variables.TryGetValue(name, out var variable))
                    {
                        errors.Add($"Unknown variable '{{{name}}}' at position {i}");
                    }
                    else if (colon >= 0)
                    {
                        var spec = token[(colon + 1)..];

                        var specError = variable.SpecValidator(spec);

                        if (specError is not null)
                            errors.Add($"Invalid spec for '{{{name}:{spec}}}' at position {i}: {specError}");
                    }

                    i = close + 1;
                    break;
                }

                case '}':
                    errors.Add($"Unmatched '}}' at position {i}");
                    i++;
                    break;

                default:
                    i++;
                    break;
            }
        }

        if (bracketDepth > 0)
            errors.Add($"{bracketDepth} unclosed '['");

        return errors;
    }

    public bool IsValid(string format) => Validate(format).Count == 0;

}
