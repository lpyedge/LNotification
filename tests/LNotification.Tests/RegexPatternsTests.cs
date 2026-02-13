using LNotification.Internal;
using Xunit;

namespace LNotification.Tests;

public class RegexPatternsTests
{
    // ─── EscapeTelegramMarkdown ───

    [Fact]
    public void EscapeTelegramMarkdown_PreservesProtectedSegments()
    {
        var input = "**bold** and `code`";

        var escaped = RegexPatterns.EscapeTelegramMarkdown(input);

        Assert.Contains("**bold**", escaped);
        Assert.Contains("`code`", escaped);
    }

    [Fact]
    public void EscapeTelegramMarkdown_EscapesSpecialCharacters()
    {
        var input = "a_b";

        var escaped = RegexPatterns.EscapeTelegramMarkdown(input);

        Assert.Equal("a\\_b", escaped);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EscapeTelegramMarkdown_EmptyOrNullInput_ReturnsEmpty(string? input)
    {
        var result = RegexPatterns.EscapeTelegramMarkdown(input!);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void EscapeTelegramMarkdown_CodeBlock_ContentPreserved()
    {
        // Code blocks should be preserved as-is, even if they contain special chars
        var input = "text ```var x_y = 1;``` more";

        var escaped = RegexPatterns.EscapeTelegramMarkdown(input);

        Assert.Contains("```var x_y = 1;```", escaped);
    }

    [Fact]
    public void EscapeTelegramMarkdown_Link_Preserved()
    {
        var input = "see [click here](https://example.com) end";

        var escaped = RegexPatterns.EscapeTelegramMarkdown(input);

        Assert.Contains("[click here](https://example.com)", escaped);
    }

    // ─── StripMarkdown ───

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StripMarkdown_EmptyOrNullInput_ReturnsEmpty(string? input)
    {
        var result = RegexPatterns.StripMarkdown(input!);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void StripMarkdown_RemovesBoldAndItalic()
    {
        var input = "**bold** and *italic* and _underscore_";

        var result = RegexPatterns.StripMarkdown(input);

        Assert.Contains("bold", result);
        Assert.Contains("italic", result);
        Assert.Contains("underscore", result);
        Assert.DoesNotContain("**", result);
        Assert.DoesNotContain("*italic*", result);
    }

    [Fact]
    public void StripMarkdown_RemovesHeaders()
    {
        var input = "# Header\nSome text";

        var result = RegexPatterns.StripMarkdown(input);

        Assert.Contains("Header", result);
        Assert.Contains("Some text", result);
        Assert.DoesNotContain("#", result);
    }

    [Fact]
    public void StripMarkdown_ExtractsLinkText()
    {
        var input = "[example](https://example.com)";

        var result = RegexPatterns.StripMarkdown(input);

        Assert.Contains("example", result);
        Assert.DoesNotContain("https://", result);
    }

    // ─── MarkdownToHtml ───

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkdownToHtml_EmptyOrNullInput_ReturnsEmpty(string? input)
    {
        var result = RegexPatterns.MarkdownToHtml(input!);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void MarkdownToHtml_ConvertsBoldToStrong()
    {
        var input = "**bold text**";

        var result = RegexPatterns.MarkdownToHtml(input);

        Assert.Contains("<strong>", result);
        Assert.Contains("bold text", result);
    }

    [Fact]
    public void MarkdownToHtml_ConvertsInlineCode()
    {
        var input = "use `foo()` here";

        var result = RegexPatterns.MarkdownToHtml(input);

        Assert.Contains("<code>", result);
        Assert.Contains("foo()", result);
    }
}
