using System.Text;
using Microsoft.Extensions.Logging;
using NotepadCommander.Core.Models;

namespace NotepadCommander.Core.Services;

public class FileService : IFileService
{
    private readonly ILogger<FileService> _logger;
    private static int _untitledCounter;

    private static readonly Dictionary<string, SupportedLanguage> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".cs"] = SupportedLanguage.CSharp,
        [".js"] = SupportedLanguage.JavaScript,
        [".ts"] = SupportedLanguage.TypeScript,
        [".tsx"] = SupportedLanguage.TypeScript,
        [".jsx"] = SupportedLanguage.JavaScript,
        [".py"] = SupportedLanguage.Python,
        [".java"] = SupportedLanguage.Java,
        [".cpp"] = SupportedLanguage.Cpp,
        [".cc"] = SupportedLanguage.Cpp,
        [".cxx"] = SupportedLanguage.Cpp,
        [".h"] = SupportedLanguage.C,
        [".c"] = SupportedLanguage.C,
        [".html"] = SupportedLanguage.Html,
        [".htm"] = SupportedLanguage.Html,
        [".css"] = SupportedLanguage.Css,
        [".xml"] = SupportedLanguage.Xml,
        [".xaml"] = SupportedLanguage.Xml,
        [".axaml"] = SupportedLanguage.Xml,
        [".csproj"] = SupportedLanguage.Xml,
        [".sln"] = SupportedLanguage.Xml,
        [".json"] = SupportedLanguage.Json,
        [".yaml"] = SupportedLanguage.Yaml,
        [".yml"] = SupportedLanguage.Yaml,
        [".md"] = SupportedLanguage.Markdown,
        [".sql"] = SupportedLanguage.Sql,
        [".ps1"] = SupportedLanguage.PowerShell,
        [".sh"] = SupportedLanguage.Bash,
        [".bash"] = SupportedLanguage.Bash,
        [".rb"] = SupportedLanguage.Ruby,
        [".go"] = SupportedLanguage.Go,
        [".rs"] = SupportedLanguage.Rust,
        [".php"] = SupportedLanguage.Php,
    };

    public FileService(ILogger<FileService> logger)
    {
        _logger = logger;
    }

    public TextDocument CreateNew()
    {
        _untitledCounter++;
        return new TextDocument
        {
            Content = string.Empty,
            FilePath = null,
            Encoding = DocumentEncoding.Utf8,
            LineEnding = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? LineEndingType.CrLf
                : LineEndingType.Lf,
            Language = SupportedLanguage.PlainText,
            IsModified = false
        };
    }

    public async Task<TextDocument> OpenAsync(string filePath)
    {
        _logger.LogDebug("Ouverture du fichier : {FilePath}", filePath);

        var data = await File.ReadAllBytesAsync(filePath);
        var encoding = DetectEncoding(data);
        var dotNetEncoding = GetDotNetEncoding(encoding);
        var content = dotNetEncoding.GetString(data);

        // Retirer le BOM si present
        if (content.Length > 0 && content[0] == '\uFEFF')
        {
            content = content[1..];
        }

        var lineEnding = DetectLineEnding(content);
        var language = DetectLanguage(filePath);

        return new TextDocument
        {
            Content = content,
            FilePath = filePath,
            Encoding = encoding,
            LineEnding = lineEnding,
            Language = language,
            IsModified = false
        };
    }

    public async Task SaveAsync(TextDocument document)
    {
        if (document.FilePath == null)
            throw new InvalidOperationException("Le document n'a pas de chemin de fichier. Utilisez SaveAs.");

        await SaveAsAsync(document, document.FilePath);
    }

    public async Task SaveAsAsync(TextDocument document, string filePath)
    {
        _logger.LogDebug("Sauvegarde du fichier : {FilePath}", filePath);

        var encoding = GetDotNetEncoding(document.Encoding);
        var content = NormalizeLineEndings(document.Content, document.LineEnding);

        await File.WriteAllTextAsync(filePath, content, encoding);

        document.FilePath = filePath;
        document.IsModified = false;
        document.Language = DetectLanguage(filePath);
    }

    public DocumentEncoding DetectEncoding(byte[] data)
    {
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            return DocumentEncoding.Utf8Bom;

        if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE)
            return DocumentEncoding.Utf16Le;

        if (data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF)
            return DocumentEncoding.Utf16Be;

        // Verifier si c'est du pur ASCII
        var isAscii = true;
        foreach (var b in data)
        {
            if (b > 127)
            {
                isAscii = false;
                break;
            }
        }

        if (isAscii)
            return DocumentEncoding.Utf8; // ASCII est un sous-ensemble d'UTF-8

        // Essayer UTF-8
        try
        {
            var utf8 = new UTF8Encoding(false, true);
            utf8.GetString(data);
            return DocumentEncoding.Utf8;
        }
        catch
        {
            return DocumentEncoding.Latin1;
        }
    }

    public LineEndingType DetectLineEnding(string content)
    {
        var crlfCount = 0;
        var lfCount = 0;
        var crCount = 0;

        for (var i = 0; i < content.Length; i++)
        {
            if (content[i] == '\r')
            {
                if (i + 1 < content.Length && content[i + 1] == '\n')
                {
                    crlfCount++;
                    i++;
                }
                else
                {
                    crCount++;
                }
            }
            else if (content[i] == '\n')
            {
                lfCount++;
            }
        }

        if (crlfCount >= lfCount && crlfCount >= crCount)
            return LineEndingType.CrLf;
        if (lfCount >= crCount)
            return LineEndingType.Lf;
        return LineEndingType.Cr;
    }

    public SupportedLanguage DetectLanguage(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return ExtensionMap.TryGetValue(extension, out var language)
            ? language
            : SupportedLanguage.PlainText;
    }

    public System.Text.Encoding GetDotNetEncoding(DocumentEncoding encoding) => encoding switch
    {
        DocumentEncoding.Utf8 => new UTF8Encoding(false),
        DocumentEncoding.Utf8Bom => new UTF8Encoding(true),
        DocumentEncoding.Ascii => System.Text.Encoding.ASCII,
        DocumentEncoding.Latin1 => System.Text.Encoding.Latin1,
        DocumentEncoding.Utf16Le => System.Text.Encoding.Unicode,
        DocumentEncoding.Utf16Be => System.Text.Encoding.BigEndianUnicode,
        _ => new UTF8Encoding(false)
    };

    private static string NormalizeLineEndings(string content, LineEndingType lineEnding)
    {
        // Normaliser d'abord en LF
        var normalized = content.Replace("\r\n", "\n").Replace("\r", "\n");

        return lineEnding switch
        {
            LineEndingType.CrLf => normalized.Replace("\n", "\r\n"),
            LineEndingType.Cr => normalized.Replace("\n", "\r"),
            _ => normalized
        };
    }
}
