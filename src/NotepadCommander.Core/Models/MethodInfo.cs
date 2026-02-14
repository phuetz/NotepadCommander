namespace NotepadCommander.Core.Models;

public class MethodInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string FullBody { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string Language { get; set; } = string.Empty;
}
