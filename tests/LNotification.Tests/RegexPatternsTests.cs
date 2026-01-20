using LNotification.Internal;
using Xunit;

namespace LNotification.Tests;

public class RegexPatternsTests
{
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
}
