using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace NotepadCommander.UI.Controls;

public static class MarkdownRenderer
{
    private static readonly FontFamily MonoFont = new("Cascadia Code, Consolas, Menlo, monospace");

    public static List<Control> RenderHtml(string html)
    {
        var controls = new List<Control>();
        if (string.IsNullOrWhiteSpace(html)) return controls;

        // Split by block-level tags
        var blocks = Regex.Split(html, @"(?=<(?:h[1-6]|p|pre|ul|ol|hr|blockquote)[>\s])");

        foreach (var block in blocks)
        {
            if (string.IsNullOrWhiteSpace(block)) continue;
            var control = RenderBlock(block.Trim());
            if (control != null)
                controls.Add(control);
        }

        return controls;
    }

    private static Control? RenderBlock(string block)
    {
        // Headings
        var headingMatch = Regex.Match(block, @"<h([1-6])>(.*?)</h\1>", RegexOptions.Singleline);
        if (headingMatch.Success)
        {
            var level = int.Parse(headingMatch.Groups[1].Value);
            var text = StripTags(headingMatch.Groups[2].Value);
            return new TextBlock
            {
                Text = text,
                FontSize = level switch { 1 => 24, 2 => 20, 3 => 16, 4 => 14, _ => 13 },
                FontWeight = FontWeight.Bold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, level <= 2 ? 16 : 12, 0, 8)
            };
        }

        // Horizontal rule
        if (block.Contains("<hr"))
        {
            return new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.Parse("#CCCCCC")),
                Margin = new Thickness(0, 8, 0, 8)
            };
        }

        // Code block
        var preMatch = Regex.Match(block, @"<pre><code[^>]*>(.*?)</code></pre>", RegexOptions.Singleline);
        if (preMatch.Success)
        {
            var code = DecodeHtml(preMatch.Groups[1].Value);
            return new Border
            {
                Background = new SolidColorBrush(Color.Parse("#F5F5F5")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12, 8),
                Margin = new Thickness(0, 4, 0, 4),
                Child = new TextBlock
                {
                    Text = code,
                    FontFamily = MonoFont,
                    FontSize = 13,
                    TextWrapping = TextWrapping.NoWrap,
                    Foreground = new SolidColorBrush(Color.Parse("#333333"))
                }
            };
        }

        // Blockquote
        var quoteMatch = Regex.Match(block, @"<blockquote>(.*?)</blockquote>", RegexOptions.Singleline);
        if (quoteMatch.Success)
        {
            var text = StripTags(quoteMatch.Groups[1].Value);
            return new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#4080C0")),
                BorderThickness = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(12, 4, 4, 4),
                Margin = new Thickness(0, 4, 0, 4),
                Child = new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.Parse("#666666")),
                    FontStyle = FontStyle.Italic
                }
            };
        }

        // Unordered list
        var ulMatch = Regex.Match(block, @"<ul>(.*?)</ul>", RegexOptions.Singleline);
        if (ulMatch.Success)
        {
            return RenderList(ulMatch.Groups[1].Value, ordered: false);
        }

        // Ordered list
        var olMatch = Regex.Match(block, @"<ol>(.*?)</ol>", RegexOptions.Singleline);
        if (olMatch.Success)
        {
            return RenderList(olMatch.Groups[1].Value, ordered: true);
        }

        // Paragraph
        var pMatch = Regex.Match(block, @"<p>(.*?)</p>", RegexOptions.Singleline);
        if (pMatch.Success)
        {
            var text = StripTags(pMatch.Groups[1].Value);
            if (string.IsNullOrWhiteSpace(text)) return null;
            return new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 4),
                LineHeight = 20
            };
        }

        // Fallback: render as plain text if it has content
        var plainText = StripTags(block);
        if (!string.IsNullOrWhiteSpace(plainText))
        {
            return new TextBlock
            {
                Text = plainText,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 2)
            };
        }

        return null;
    }

    private static Control RenderList(string listHtml, bool ordered)
    {
        var panel = new StackPanel { Spacing = 2, Margin = new Thickness(0, 4, 0, 4) };
        var items = Regex.Matches(listHtml, @"<li>(.*?)</li>", RegexOptions.Singleline);

        var index = 1;
        foreach (Match item in items)
        {
            var text = StripTags(item.Groups[1].Value);
            var bullet = ordered ? $"  {index++}. " : "  \u2022  ";
            var itemPanel = new StackPanel { Orientation = Orientation.Horizontal };
            itemPanel.Children.Add(new TextBlock
            {
                Text = bullet,
                Foreground = new SolidColorBrush(Color.Parse("#666666")),
                VerticalAlignment = VerticalAlignment.Top
            });
            itemPanel.Children.Add(new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top
            });
            panel.Children.Add(itemPanel);
        }

        return panel;
    }

    private static string StripTags(string html)
    {
        var text = Regex.Replace(html, @"<[^>]+>", "");
        return DecodeHtml(text).Trim();
    }

    private static string DecodeHtml(string html)
    {
        return html
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Replace("&#39;", "'")
            .Replace("&nbsp;", " ");
    }
}
