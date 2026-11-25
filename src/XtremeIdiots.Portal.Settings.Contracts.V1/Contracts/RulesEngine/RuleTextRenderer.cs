using System.Text;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;

public sealed record RuleTextError(string Code, int Position, string Message);
public sealed record RuleTextDiagnostic(string Code, string? TokenKey, RuleTextTokenSource? Source, string Message);
internal sealed record RuleTextPart(string Text, RuleTextTokenDefinition? Token = null);

public sealed class RuleTextParseResult
{
    internal IReadOnlyList<RuleTextPart> Parts { get; init; } = [];
    public IReadOnlyList<RuleTextTokenDefinition> Tokens { get; init; } = [];
    public IReadOnlyList<RuleTextTokenSource> RequiredSources { get; init; } = [];
    public IReadOnlyList<RuleTextError> Errors { get; init; } = [];
    public bool IsValid => Errors.Count == 0;
}

public sealed record RuleTextRenderResult(
    string? Text,
    IReadOnlyList<RuleTextDiagnostic> Diagnostics,
    IReadOnlyList<RuleTextError> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

/// <summary>Opt-in template processing only. Literal parameters must not pass through this renderer.</summary>
public static class RuleTextRenderer
{
    public static IEnumerable<string> Validate(string template) =>
        Parse(template).Errors.Select(error => $"{error.Code} at position {error.Position}: {error.Message}");

    public static RuleTextParseResult Parse(string template)
    {
        ArgumentNullException.ThrowIfNull(template);
        var parts = new List<RuleTextPart>();
        var tokens = new List<RuleTextTokenDefinition>();
        var errors = new List<RuleTextError>();
        var literal = new StringBuilder();
        for (var index = 0; index < template.Length;)
        {
            var character = template[index];
            if (character is not ('{' or '}'))
            {
                literal.Append(character);
                index++;
                continue;
            }

            if (index + 1 < template.Length && template[index + 1] == character)
            {
                literal.Append(character);
                index += 2;
                continue;
            }

            if (character == '}')
            {
                errors.Add(new("MalformedToken", index, "Unescaped closing brace."));
                index++;
                continue;
            }

            var start = index++;
            var keyStart = index;
            while (index < template.Length && template[index] is not ('{' or '}'))
            {
                index++;
            }

            if (index == template.Length || template[index] == '{')
            {
                errors.Add(new("MalformedToken", start, "Token must have one matching closing brace and no nested braces."));
                continue;
            }

            var key = template[keyStart..index++];
            if (key.Length == 0 || key.Any(character => !(char.IsAsciiLetterOrDigit(character) || character == '.')))
            {
                errors.Add(new("MalformedToken", start, "Expected a registered token identifier without whitespace or expressions."));
                continue;
            }

            var definition = RuleTextTokens.GetByKey(key);
            if (definition is null)
            {
                errors.Add(new("UnknownToken", start, $"Unknown text token '{key}'."));
                continue;
            }

            if (literal.Length > 0)
            {
                parts.Add(new(literal.ToString()));
                literal.Clear();
            }
            parts.Add(new(string.Empty, definition));
            if (!tokens.Contains(definition))
            {
                tokens.Add(definition);
            }
        }

        if (literal.Length > 0)
        {
            parts.Add(new(literal.ToString()));
        }

        return new()
        {
            Parts = parts.AsReadOnly(),
            Tokens = tokens.AsReadOnly(),
            RequiredSources = Array.AsReadOnly(tokens.SelectMany(token => token.RequiredSources).Distinct().ToArray()),
            Errors = errors.AsReadOnly()
        };
    }

    public static RuleTextRenderResult Render(string template, RuleTextValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var parsed = Parse(template);
        if (!parsed.IsValid)
        {
            return new(null, [], parsed.Errors);
        }

        var diagnostics = new List<RuleTextDiagnostic>();
        foreach (var source in parsed.RequiredSources)
        {
            if (values.SourceDiagnostics.TryGetValue(source, out var message))
            {
                diagnostics.Add(new("SourceUnavailable", null, source, message));
            }
        }

        var text = new StringBuilder();
        var reportedFallbacks = new HashSet<string>(StringComparer.Ordinal);
        foreach (var part in parsed.Parts)
        {
            if (part.Token is not { } token)
            {
                text.Append(part.Text);
                continue;
            }

            var value = RuleTextTokens.Resolve(token.Key, values);
            if (string.IsNullOrWhiteSpace(value))
            {
                value = token.Fallback;
                if (reportedFallbacks.Add(token.Key))
                {
                    diagnostics.Add(new("FallbackUsed", token.Key, token.RequiredSources[0],
                        "Display value unavailable; the catalogue fallback was used."));
                }
            }
            text.Append(value);
        }

        return new(text.ToString(), diagnostics.AsReadOnly(), []);
    }
}
