using Markdig;
using Microsoft.AspNetCore.Components;

namespace FaultDesk.Web.Services;

/// <summary>
/// Renders model-generated markdown. Raw HTML is disabled because the text comes from an LLM that has read
/// arbitrary web pages; links open in a new tab.
/// </summary>
public sealed class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    public MarkupString ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return new MarkupString(string.Empty);
        }

        var html = Markdown.ToHtml(markdown, Pipeline)
            .Replace("<a href=", "<a target=\"_blank\" rel=\"noopener noreferrer\" href=", StringComparison.Ordinal);

        return new MarkupString(html);
    }
}
