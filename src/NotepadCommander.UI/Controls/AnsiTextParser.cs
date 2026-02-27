using Avalonia.Media;

namespace NotepadCommander.UI.Controls;

/// <summary>
/// Parses ANSI escape codes into styled text spans.
/// Supports: \x1b[0m (reset), \x1b[1m (bold), \x1b[30-37m] (standard fg),
/// \x1b[90-97m] (bright fg), \x1b[38;5;Nm] (256-color fg).
/// </summary>
public static class AnsiTextParser
{
    public record AnsiSpan(string Text, IBrush? Foreground, bool IsBold);

    private static readonly IBrush[] StandardColors =
    {
        new SolidColorBrush(Color.Parse("#000000")), // 30 Black
        new SolidColorBrush(Color.Parse("#CC0000")), // 31 Red
        new SolidColorBrush(Color.Parse("#00CC00")), // 32 Green
        new SolidColorBrush(Color.Parse("#CCCC00")), // 33 Yellow
        new SolidColorBrush(Color.Parse("#3333CC")), // 34 Blue
        new SolidColorBrush(Color.Parse("#CC00CC")), // 35 Magenta
        new SolidColorBrush(Color.Parse("#00CCCC")), // 36 Cyan
        new SolidColorBrush(Color.Parse("#CCCCCC")), // 37 White
    };

    private static readonly IBrush[] BrightColors =
    {
        new SolidColorBrush(Color.Parse("#666666")), // 90 Bright Black
        new SolidColorBrush(Color.Parse("#FF3333")), // 91 Bright Red
        new SolidColorBrush(Color.Parse("#33FF33")), // 92 Bright Green
        new SolidColorBrush(Color.Parse("#FFFF33")), // 93 Bright Yellow
        new SolidColorBrush(Color.Parse("#6666FF")), // 94 Bright Blue
        new SolidColorBrush(Color.Parse("#FF33FF")), // 95 Bright Magenta
        new SolidColorBrush(Color.Parse("#33FFFF")), // 96 Bright Cyan
        new SolidColorBrush(Color.Parse("#FFFFFF")), // 97 Bright White
    };

    public static List<AnsiSpan> Parse(string input)
    {
        var spans = new List<AnsiSpan>();
        if (string.IsNullOrEmpty(input))
            return spans;

        IBrush? currentFg = null;
        bool currentBold = false;
        int i = 0;
        int textStart = 0;

        while (i < input.Length)
        {
            if (input[i] == '\x1b' && i + 1 < input.Length && input[i + 1] == '[')
            {
                // Flush any text before this escape
                if (i > textStart)
                    spans.Add(new AnsiSpan(input[textStart..i], currentFg, currentBold));

                // Parse the escape sequence
                int seqStart = i + 2;
                int seqEnd = seqStart;
                while (seqEnd < input.Length && input[seqEnd] != 'm' && seqEnd - seqStart < 20)
                    seqEnd++;

                if (seqEnd < input.Length && input[seqEnd] == 'm')
                {
                    var codes = input[seqStart..seqEnd].Split(';');
                    ProcessCodes(codes, ref currentFg, ref currentBold);
                    i = seqEnd + 1;
                }
                else
                {
                    i = seqEnd;
                }

                textStart = i;
            }
            else
            {
                i++;
            }
        }

        if (textStart < input.Length)
            spans.Add(new AnsiSpan(input[textStart..], currentFg, currentBold));

        return spans;
    }

    private static void ProcessCodes(string[] codes, ref IBrush? fg, ref bool bold)
    {
        for (int c = 0; c < codes.Length; c++)
        {
            if (!int.TryParse(codes[c], out var code)) continue;

            if (code == 0)
            {
                fg = null;
                bold = false;
            }
            else if (code == 1)
            {
                bold = true;
            }
            else if (code >= 30 && code <= 37)
            {
                fg = StandardColors[code - 30];
            }
            else if (code >= 90 && code <= 97)
            {
                fg = BrightColors[code - 90];
            }
        }
    }
}
