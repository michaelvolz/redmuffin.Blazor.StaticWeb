using Markdig;
using Microsoft.AspNetCore.Components;

namespace redmuffin.Blazor.StaticWeb.Features.MarkdownExamplesPage;

public partial class MarkdownExamples : ComponentBase
{
    private MarkupString _markdownText = new("n/a");

    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
#pragma warning disable MA0015 // Not a method parameter — validating Blazor [Inject] property
        ArgumentNullException.ThrowIfNull(HttpClientFactory);
#pragma warning restore MA0015
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        using var httpClient = HttpClientFactory.CreateClient();
        _markdownText = new MarkupString(Markdown.ToHtml(await httpClient.GetStringAsync("Example.md").ConfigureAwait(false), pipeline));
    }
}