using XtremeIdiots.Portal.Repository.Api.V1.Validation;
using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Validation;

public class TagHtmlSanitizerTests
{
    [Fact]
    public void TrySanitize_WithValidSpanAndIcon_ReturnsSanitizedHtml()
    {
        const string input = "<span class=\"badge bg-warning\"><i class=\"fa-solid fa-shield\"></i> Moderate Chat</span>";

        var isValid = TagHtmlSanitizer.TrySanitize(input, out var sanitized, out var error);

        Assert.True(isValid);
        Assert.Null(error);
        Assert.Equal("<span class=\"badge bg-warning\"><i class=\"fa-solid fa-shield\"></i> Moderate Chat</span>", sanitized);
    }

    [Fact]
    public void TrySanitize_WithScriptMarkup_ReturnsFalse()
    {
        const string input = "<script>alert('xss')</script>";

        var isValid = TagHtmlSanitizer.TrySanitize(input, out var sanitized, out var error);

        Assert.False(isValid);
        Assert.NotNull(error);
        Assert.Equal(input, sanitized);
    }

    [Fact]
    public void TrySanitize_WithInvalidClassToken_ReturnsFalse()
    {
        const string input = "<span class=\"badge bg-warning()\">Test</span>";

        var isValid = TagHtmlSanitizer.TrySanitize(input, out _, out var error);

        Assert.False(isValid);
        Assert.NotNull(error);
    }

    [Fact]
    public void TrySanitize_WithPlainLabelSpecialCharacters_EncodesLabel()
    {
        const string input = "<span class=\"badge bg-warning\">A & B</span>";

        var isValid = TagHtmlSanitizer.TrySanitize(input, out var sanitized, out var error);

        Assert.True(isValid);
        Assert.Null(error);
        Assert.Equal("<span class=\"badge bg-warning\">A &amp; B</span>", sanitized);
    }
}
