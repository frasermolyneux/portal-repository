using System.Net;
using System.Text.RegularExpressions;

namespace XtremeIdiots.Portal.Repository.Api.V1.Validation;

internal static partial class TagHtmlSanitizer
{
    private static readonly Regex SpanWithOptionalIconPattern = SpanWithOptionalIconRegex();
    private static readonly Regex ClassTokenPattern = ClassTokenRegex();

    public static bool TrySanitize(string? input, out string? sanitized, out string? error)
    {
        sanitized = input;
        error = null;

        if (input is null)
        {
            return true;
        }

        var trimmed = input.Trim();
        if (trimmed.Length == 0)
        {
            sanitized = null;
            return true;
        }

        var match = SpanWithOptionalIconPattern.Match(trimmed);
        if (!match.Success)
        {
            error = "TagHtml must be a <span class=\"...\"> label, with an optional <i class=\"...\"></i> icon.";
            return false;
        }

        var spanClasses = NormalizeClasses(match.Groups[1].Value);
        if (spanClasses is null)
        {
            error = "TagHtml contains invalid CSS class names on the span element.";
            return false;
        }

        var iconClassesRaw = match.Groups[2].Success ? match.Groups[3].Value : null;
        var iconClasses = NormalizeClasses(iconClassesRaw);
        if (iconClassesRaw is not null && iconClasses is null)
        {
            error = "TagHtml contains invalid CSS class names on the icon element.";
            return false;
        }

        var label = WebUtility.HtmlEncode(match.Groups[4].Value.Trim());
        if (string.IsNullOrWhiteSpace(label))
        {
            error = "TagHtml must include non-empty label text.";
            return false;
        }

        sanitized = iconClasses is null
            ? $"<span class=\"{spanClasses}\">{label}</span>"
            : $"<span class=\"{spanClasses}\"><i class=\"{iconClasses}\"></i> {label}</span>";

        return true;
    }

    private static string? NormalizeClasses(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var tokens = raw
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (tokens.Count == 0 || tokens.Any(t => !ClassTokenPattern.IsMatch(t)))
        {
            return null;
        }

        return string.Join(' ', tokens);
    }

    [GeneratedRegex(
        "^<span\\s+class=\"([^\"]+)\"\\s*>\\s*(<i\\s+class=\"([^\"]+)\"\\s*><\\/i>\\s*)?([^<>]+?)\\s*<\\/span>$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SpanWithOptionalIconRegex();

    [GeneratedRegex("^[a-zA-Z0-9_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex ClassTokenRegex();
}
