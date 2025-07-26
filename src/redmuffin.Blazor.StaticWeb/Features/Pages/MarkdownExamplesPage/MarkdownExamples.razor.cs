using Markdig;
using Microsoft.AspNetCore.Components;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.MarkdownExamplesPage;

public partial class MarkdownExamples : ComponentBase
{
    private MarkupString _markdownText = new("n/a");

    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        ArgumentNullException.ThrowIfNull(HttpClientFactory);
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        using var httpClient = HttpClientFactory.CreateClient();
        _markdownText = new MarkupString(Markdown.ToHtml(await httpClient.GetStringAsync("Example.md").ConfigureAwait(false), pipeline));
    }
}