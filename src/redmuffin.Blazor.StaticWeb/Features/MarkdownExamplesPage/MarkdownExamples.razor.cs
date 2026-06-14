using Markdig;
using Microsoft.AspNetCore.Components;

namespace redmuffin.Blazor.StaticWeb.Features.MarkdownExamplesPage;

public partial class MarkdownExamples : ComponentBase
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