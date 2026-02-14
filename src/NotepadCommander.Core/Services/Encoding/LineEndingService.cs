using NotepadCommander.Core.Models;

namespace NotepadCommander.Core.Services.Encoding;

public class LineEndingService : ILineEndingService
{
    public string ConvertLineEndings(string text, LineEndingType targetType)
    {
        // Normaliser en LF d'abord
        var normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");

        return targetType switch
        {
            LineEndingType.CrLf => normalized.Replace("\n", "\r\n"),
            LineEndingType.Cr => normalized.Replace("\n", "\r"),
            LineEndingType.Lf => normalized,
            _ => normalized
        };
    }

    public LineEndingType DetectLineEnding(string text)
    {
        var crlfCount = 0;
        var lfCount = 0;
        var crCount = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    crlfCount++;
                    i++;
                }
                else
                {
                    crCount++;
                }
            }
            else if (text[i] == '\n')
            {
                lfCount++;
            }
        }

        if (crlfCount >= lfCount && crlfCount >= crCount) return LineEndingType.CrLf;
        if (lfCount >= crCount) return LineEndingType.Lf;
        return LineEndingType.Cr;
    }
}
