using Markdig;
using Microsoft.AspNetCore.Components;

namespace redmuffin.Blazor.StaticWeb.Pages.MarkdownExamples;

#pragma warning disable MA0049 // Type name matches namespace — standard Blazor component pattern
public partial class MarkdownExamples : ComponentBase
#pragma warning restore MA0049
{
    private readonly IHttpClientFactory _httpClientFactory;
    private MarkupString _markdownText = new("n/a");

    public MarkdownExamples(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    protected override async Task OnInitializedAsync()
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        using var httpClient = _httpClientFactory.CreateClient();
        _markdownText = new MarkupString(Markdown.ToHtml(await httpClient.GetStringAsync("Example.md").ConfigureAwait(false), pipeline));
    }
}