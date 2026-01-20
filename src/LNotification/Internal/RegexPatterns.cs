using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace LNotification.Internal;

internal static partial class RegexPatterns
{
    private static readonly char[] TelegramEscapeChars =
    {
        '_', '*', '[', ']', '(', ')', '~', '`',
        '>', '#', '+', '-', '=', '|', '{', '}', '.', '!'
    };

    [GeneratedRegex(@"```[\s\S]*?```", RegexOptions.Singleline)]
    private static partial Regex TgCodeBlockRegex();

    [GeneratedRegex(@"`[^`]+`")]
    private static partial Regex TgInlineCodeRegex();

    [GeneratedRegex(@"\[[^\]]+\]\([^)]+\)")]
    private static partial Regex TgLinkRegex();

    [GeneratedRegex(@"\*\*[^*]+\*\*")]
    private static partial Regex TgBold1Regex();

    [GeneratedRegex(@"__[^_]+__")]
    private static partial Regex TgBold2Regex();

    [GeneratedRegex(@"\*[^*]+\*")]
    private static partial Regex TgItalic1Regex();

    [GeneratedRegex(@"_[^_]+_")]
    private static partial Regex TgItalic2Regex();

    [GeneratedRegex(@"~[^~]+~")]
    private static partial Regex TgStrikeRegex();

    internal static string EscapeTelegramMarkdown(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var placeholders = new Dictionary<string, string>();
        var index = 0;

        string Protect(Regex regex)
        {
            input = regex.Replace(input, m =>
            {
                var key = $"%%TGPLACEHOLDER{index++}%%";
                placeholders[key] = m.Value;
                return key;
            });
            return input;
        }

        Protect(TgCodeBlockRegex());
        Protect(TgInlineCodeRegex());
        Protect(TgLinkRegex());
        Protect(TgBold1Regex());
        Protect(TgBold2Regex());
        Protect(TgItalic1Regex());
        Protect(TgItalic2Regex());
        Protect(TgStrikeRegex());

        var sb = new StringBuilder(input.Length * 2);
        foreach (var ch in input)
        {
            if (TelegramEscapeChars.Contains(ch))
            {
                sb.Append('\\');
            }
            sb.Append(ch);
        }

        input = sb.ToString();

        foreach (var kv in placeholders)
        {
            input = input.Replace(kv.Key, kv.Value);
        }

        return input;
    }

    internal static string StripMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var text = markdown;

        text = StripCodeBlockRegex().Replace(text, string.Empty);
        text = StripInlineCodeRegex().Replace(text, "$1");
        text = StripBoldRegex1().Replace(text, "$1");
        text = StripBoldRegex2().Replace(text, "$1");
        text = StripItalicRegex1().Replace(text, "$1");
        text = StripItalicRegex2().Replace(text, "$1");
        text = StripHeaderRegex().Replace(text, string.Empty);
        text = StripLinkRegex().Replace(text, "$1");
        text = StripBlockquoteRegex().Replace(text, string.Empty);
        text = StripUnorderedListRegex().Replace(text, string.Empty);
        text = StripOrderedListRegex().Replace(text, string.Empty);

        return WebUtility.HtmlDecode(text).Trim();
    }

    internal static string MarkdownToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var html = WebUtility.HtmlEncode(markdown);

        html = HtmlCodeBlockRegex().Replace(html, "<pre><code>$1</code></pre>");
        html = HtmlInlineCodeRegex().Replace(html, "<code>$1</code>");
        html = HtmlBoldRegex1().Replace(html, "<strong>$1</strong>");
        html = HtmlBoldRegex2().Replace(html, "<strong>$1</strong>");
        html = HtmlItalicRegex1().Replace(html, "<em>$1</em>");
        html = HtmlItalicRegex2().Replace(html, "<em>$1</em>");
        html = HtmlHeader3Regex().Replace(html, "<h3>$1</h3>");
        html = HtmlHeader2Regex().Replace(html, "<h2>$1</h2>");
        html = HtmlHeader1Regex().Replace(html, "<h1>$1</h1>");
        html = HtmlLinkRegex().Replace(html, "<a href=\"$2\">$1</a>");
        html = html.Replace("\r\n", "<br>").Replace("\n", "<br>");

        return html;
    }

    [GeneratedRegex(@"```[\s\S]*?```", RegexOptions.Singleline)]
    private static partial Regex StripCodeBlockRegex();

    [GeneratedRegex(@"`([^`]*)`")]
    private static partial Regex StripInlineCodeRegex();

    [GeneratedRegex(@"\*\*(.*?)\*\*")]
    private static partial Regex StripBoldRegex1();

    [GeneratedRegex(@"__(.*?)__")]
    private static partial Regex StripBoldRegex2();

    [GeneratedRegex(@"\*(.*?)\*")]
    private static partial Regex StripItalicRegex1();

    [GeneratedRegex(@"_(.*?)_")]
    private static partial Regex StripItalicRegex2();

    [GeneratedRegex(@"^\s{0,3}#+\s*", RegexOptions.Multiline)]
    private static partial Regex StripHeaderRegex();

    [GeneratedRegex(@"\[(.*?)\]\((.*?)\)")]
    private static partial Regex StripLinkRegex();

    [GeneratedRegex(@"^\s{0,3}>\s?", RegexOptions.Multiline)]
    private static partial Regex StripBlockquoteRegex();

    [GeneratedRegex(@"^\s*[-+*]\s+", RegexOptions.Multiline)]
    private static partial Regex StripUnorderedListRegex();

    [GeneratedRegex(@"^\s*\d+\.\s+", RegexOptions.Multiline)]
    private static partial Regex StripOrderedListRegex();

    [GeneratedRegex(@"```[\s\S]*?```", RegexOptions.Singleline)]
    private static partial Regex HtmlCodeBlockRegex();

    [GeneratedRegex(@"`([^`]*)`")]
    private static partial Regex HtmlInlineCodeRegex();

    [GeneratedRegex(@"\*\*(.*?)\*\*")]
    private static partial Regex HtmlBoldRegex1();

    [GeneratedRegex(@"__(.*?)__")]
    private static partial Regex HtmlBoldRegex2();

    [GeneratedRegex(@"\*(.*?)\*")]
    private static partial Regex HtmlItalicRegex1();

    [GeneratedRegex(@"_(.*?)_")]
    private static partial Regex HtmlItalicRegex2();

    [GeneratedRegex(@"^### (.*)$", RegexOptions.Multiline)]
    private static partial Regex HtmlHeader3Regex();

    [GeneratedRegex(@"^## (.*)$", RegexOptions.Multiline)]
    private static partial Regex HtmlHeader2Regex();

    [GeneratedRegex(@"^# (.*)$", RegexOptions.Multiline)]
    private static partial Regex HtmlHeader1Regex();

    [GeneratedRegex(@"\[(.*?)\]\((.*?)\)")]
    private static partial Regex HtmlLinkRegex();
}
