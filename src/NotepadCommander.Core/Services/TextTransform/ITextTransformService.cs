namespace NotepadCommander.Core.Services.TextTransform;

public interface ITextTransformService
{
    string SortLines(string text, bool ascending = true);
    string RemoveDuplicateLines(string text);
    string ToUpperCase(string text);
    string ToLowerCase(string text);
    string ToTitleCase(string text);
    string ToggleCase(string text);
    string TrimLines(string text);
    string RemoveEmptyLines(string text);
    string JoinLines(string text, string separator = " ");
    string ReverseLines(string text);
    string EncodeBase64(string text);
    string DecodeBase64(string text);
    string EncodeUrl(string text);
    string DecodeUrl(string text);
    string FormatJson(string text);
    string MinifyJson(string text);
    string FormatXml(string text);
}
