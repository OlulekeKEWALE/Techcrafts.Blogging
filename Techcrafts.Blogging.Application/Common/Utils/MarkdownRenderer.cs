using Ganss.Xss;
using Markdig;

namespace Techcrafts.Blogging.Application.Common.Utils;

public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private static readonly HtmlSanitizer Sanitizer = new();

    static MarkdownRenderer()
    {
        // Allowed schemes for links and images
        Sanitizer.AllowedSchemes.Clear();
        Sanitizer.AllowedSchemes.Add("http");
        Sanitizer.AllowedSchemes.Add("https");
        Sanitizer.AllowedSchemes.Add("mailto");

        // Disallow dangerous attributes and tags
        Sanitizer.RemovingAttribute += (sender, args) =>
        {
            if (args.Attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
            {
                args.Cancel = false; // do not keep on* event handlers
            }
        };
    }

    public static string RenderToSafeHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var rawHtml = Markdown.ToHtml(markdown, Pipeline);
        return Sanitizer.Sanitize(rawHtml);
    }
}
