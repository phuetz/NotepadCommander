using NotepadCommander.Core.Services.TextTransform;
using NotepadCommander.Core.Services.Compare;
using NotepadCommander.Core.Services.Encoding;

namespace NotepadCommander.Cli;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        var commandArgs = args.Skip(1).ToArray();

        return command switch
        {
            "format" => FormatCommand(commandArgs),
            "convert" => ConvertCommand(commandArgs),
            "diff" => DiffCommand(commandArgs),
            "transform" => TransformCommand(commandArgs),
            "--help" or "-h" => ShowHelp(),
            _ => Error($"Commande inconnue : {command}")
        };
    }

    static int ShowHelp()
    {
        Console.WriteLine("Notepad Commander CLI v1.0");
        Console.WriteLine();
        Console.WriteLine("Utilisation: notepad-cli <commande> [options]");
        Console.WriteLine();
        Console.WriteLine("Commandes:");
        Console.WriteLine("  format    <fichier> <type>       Formater un fichier (json, xml)");
        Console.WriteLine("  convert   <fichier> <encodage>   Convertir l'encodage (utf-8, ascii, latin-1)");
        Console.WriteLine("  diff      <fichier1> <fichier2>  Comparer deux fichiers");
        Console.WriteLine("  transform <fichier> <operation>  Transformer du texte");
        Console.WriteLine();
        Console.WriteLine("Operations de transformation:");
        Console.WriteLine("  sort-asc, sort-desc, uppercase, lowercase, titlecase,");
        Console.WriteLine("  trim, remove-empty, remove-duplicates, reverse,");
        Console.WriteLine("  encode-base64, decode-base64, encode-url, decode-url");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --help, -h    Afficher cette aide");
        Console.WriteLine("  --output, -o  Fichier de sortie (defaut: ecrasement)");
        return 0;
    }

    static int FormatCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Utilisation: notepad-cli format <fichier> <json|xml>");
            return 1;
        }

        var filePath = args[0];
        var formatType = args[1].ToLowerInvariant();
        var outputPath = GetOutputPath(args);

        if (!File.Exists(filePath))
            return Error($"Fichier introuvable : {filePath}");

        var content = File.ReadAllText(filePath);
        var service = new TextTransformService();

        string result;
        try
        {
            result = formatType switch
            {
                "json" => service.FormatJson(content),
                "xml" => service.FormatXml(content),
                _ => throw new ArgumentException($"Format non supporte : {formatType}")
            };
        }
        catch (Exception ex)
        {
            return Error($"Erreur de formatage : {ex.Message}");
        }

        File.WriteAllText(outputPath ?? filePath, result);
        Console.WriteLine($"Fichier formate ({formatType}) : {outputPath ?? filePath}");
        return 0;
    }

    static int ConvertCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Utilisation: notepad-cli convert <fichier> <encodage>");
            return 1;
        }

        var filePath = args[0];
        var targetEncoding = args[1].ToLowerInvariant();
        var outputPath = GetOutputPath(args);

        if (!File.Exists(filePath))
            return Error($"Fichier introuvable : {filePath}");

        var service = new EncodingService();
        var encoding = targetEncoding switch
        {
            "utf-8" or "utf8" => System.Text.Encoding.UTF8,
            "ascii" => System.Text.Encoding.ASCII,
            "latin-1" or "latin1" or "iso-8859-1" => System.Text.Encoding.Latin1,
            "utf-16" or "utf16" => System.Text.Encoding.Unicode,
            _ => null as System.Text.Encoding
        };

        if (encoding == null)
            return Error($"Encodage non supporte : {targetEncoding}");

        var content = File.ReadAllText(filePath);
        File.WriteAllText(outputPath ?? filePath, content, encoding);
        Console.WriteLine($"Fichier converti en {targetEncoding} : {outputPath ?? filePath}");
        return 0;
    }

    static int DiffCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Utilisation: notepad-cli diff <fichier1> <fichier2>");
            return 1;
        }

        var file1 = args[0];
        var file2 = args[1];

        if (!File.Exists(file1)) return Error($"Fichier introuvable : {file1}");
        if (!File.Exists(file2)) return Error($"Fichier introuvable : {file2}");

        var content1 = File.ReadAllText(file1);
        var content2 = File.ReadAllText(file2);

        var service = new CompareService();
        var result = service.Compare(content1, content2);

        foreach (var line in result.Lines)
        {
            var prefix = line.Type switch
            {
                DiffLineType.Inserted => "+ ",
                DiffLineType.Deleted => "- ",
                DiffLineType.Modified => "~ ",
                _ => "  "
            };

            var color = line.Type switch
            {
                DiffLineType.Inserted => ConsoleColor.Green,
                DiffLineType.Deleted => ConsoleColor.Red,
                DiffLineType.Modified => ConsoleColor.Yellow,
                _ => ConsoleColor.Gray
            };

            Console.ForegroundColor = color;
            Console.WriteLine($"{prefix}{line.Text}");
        }
        Console.ResetColor();

        var changes = result.Lines.Count(l => l.Type != DiffLineType.Unchanged);
        Console.WriteLine($"\n{changes} difference(s) trouvee(s)");
        return changes > 0 ? 1 : 0;
    }

    static int TransformCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Utilisation: notepad-cli transform <fichier> <operation>");
            return 1;
        }

        var filePath = args[0];
        var operation = args[1].ToLowerInvariant();
        var outputPath = GetOutputPath(args);

        if (!File.Exists(filePath))
            return Error($"Fichier introuvable : {filePath}");

        var content = File.ReadAllText(filePath);
        var service = new TextTransformService();

        string result;
        try
        {
            result = operation switch
            {
                "sort-asc" => service.SortLines(content, ascending: true),
                "sort-desc" => service.SortLines(content, ascending: false),
                "uppercase" => service.ToUpperCase(content),
                "lowercase" => service.ToLowerCase(content),
                "titlecase" => service.ToTitleCase(content),
                "trim" => service.TrimLines(content),
                "remove-empty" => service.RemoveEmptyLines(content),
                "remove-duplicates" => service.RemoveDuplicateLines(content),
                "reverse" => service.ReverseLines(content),
                "encode-base64" => service.EncodeBase64(content),
                "decode-base64" => service.DecodeBase64(content),
                "encode-url" => service.EncodeUrl(content),
                "decode-url" => service.DecodeUrl(content),
                _ => throw new ArgumentException($"Operation inconnue : {operation}")
            };
        }
        catch (Exception ex)
        {
            return Error($"Erreur de transformation : {ex.Message}");
        }

        File.WriteAllText(outputPath ?? filePath, result);
        Console.WriteLine($"Transformation '{operation}' appliquee : {outputPath ?? filePath}");
        return 0;
    }

    static string? GetOutputPath(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] is "--output" or "-o")
                return args[i + 1];
        }
        return null;
    }

    static int Error(string message)
    {
        Console.Error.WriteLine($"Erreur: {message}");
        return 1;
    }
}
