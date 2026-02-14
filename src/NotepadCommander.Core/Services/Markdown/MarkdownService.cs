using Markdig;

namespace NotepadCommander.Core.Services.Markdown;

public class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public string ToHtml(string markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return string.Empty;
        return Markdig.Markdown.ToHtml(markdown, _pipeline);
    }
}
