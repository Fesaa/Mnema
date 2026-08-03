using System;
using System.Collections.Generic;
using System.Text;

namespace Mnema.Common;

public delegate string? FormatVariableResolver<in T>(T context, string? formatSpec);

public class StringFormatter<T>
{

    private readonly Dictionary<string, FormatVariableResolver<T>> _variables = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Register a simple variable, spec is ignored.</summary>
    public StringFormatter<T> WithVariable(string name, Func<T, string?> resolve)
        => WithVariable(name, (ctx, _) => resolve(ctx));

    /// <summary>Register a variable whose value is post-processed by the format spec (e.g. padding).</summary>
    public StringFormatter<T> WithVariable(string name, Func<T, string?> resolve, Func<string, string, string?> applySpec)
        => WithVariable(name, (ctx, spec) =>
        {
            var value = resolve(ctx);
            return spec is not null && !string.IsNullOrEmpty(value)
                ? applySpec(value, spec)
                : value;
        });

    /// <summary>Full control — resolver sees the raw spec string and decides everything itself.</summary>
    public StringFormatter<T> WithVariable(string name, FormatVariableResolver<T> resolve)
    {
        _variables[name] = resolve;
        return this;
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

                    if (!_variables.TryGetValue(name, out var resolver))
                        throw new FormatException($"Unknown variable '{{{name}}}' in format \"{format}\"");

                    var value = resolver(context, spec);
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
                        errors.Add($"Empty variable name at position {i}");
                    else if (!_variables.ContainsKey(name))
                        errors.Add($"Unknown variable '{{{name}}}' at position {i}");

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
