using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.VideosPage;

public partial class Videos
{
    // LoggerMessage delegates for better performance
    private static readonly Action<ILogger, string, Exception> LogShimmerError =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(1, nameof(LogShimmerError)),
            "Error stopping shimmer for element: {ElementId}");

    private static readonly Action<ILogger, Exception?> LogStartingFetchVideos =
        LoggerMessage.Define(LogLevel.Information, new EventId(2, nameof(LogStartingFetchVideos)),
            "Starting to fetch videos using IRaindropAPI service");

    private static readonly Action<ILogger, Exception> LogExceptionFetchingVideos =
        LoggerMessage.Define(LogLevel.Error, new EventId(3, nameof(LogExceptionFetchingVideos)),
            "Exception occurred while fetching videos");

    private string? _errorMessage;
    private List<RaindropItem>? _videoItems;

    [Inject]
    private ILogger<Videos> Logger { get; set; } = null!;

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private IRaindropAPI RaindropAPI { get; set; } = null!;

    private static string DisplayTitle(RaindropItem video)
    {
        return string.IsNullOrEmpty(video.Title) ? "No Title Available" : video.Title;
    }

    private static string DisplayExcerpt(RaindropItem video)
    {
        return string.IsNullOrEmpty(video.Excerpt)
            ? "No Excerpt Available"
            : video.Excerpt.Length > 250
                ? string.Concat(video.Excerpt.AsSpan(0, 250), "...")
                : video.Excerpt;
    }

    protected override Task OnInitializedAsync()
    {
        // Validate injected dependencies
        ArgumentNullException.ThrowIfNull(Logger);
        ArgumentNullException.ThrowIfNull(Js);
        ArgumentNullException.ThrowIfNull(Navigation);
        ArgumentNullException.ThrowIfNull(RaindropAPI);

        // Load videos automatically when the page starts
        return FetchVideosAsync();
    }

    // Update RainDropClientId based on environment
    private string GetRainDropClientId()
    {
        var baseUri = Navigation.BaseUri.TrimEnd('/');
        return baseUri.Contains("localhost") ? "684ea82bb3333b01de5487c1" : "684c73df642469e7c1969f8e";
    }

    private async Task LoginWithRaindropAsync()
    {
        var redirectPath = "/redirect";
        var baseUri = Navigation.BaseUri.TrimEnd('/');
        var redirectUri = $"{baseUri}{redirectPath}";
        var authUrl =
            $"https://raindrop.io/oauth/authorize?client_id={GetRainDropClientId()}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code";

        await Js.InvokeVoidAsync("open", authUrl, "_self").ConfigureAwait(false);
    }

    private async Task FetchVideosAsync()
    {
        _errorMessage = null;
        _videoItems = null;
        try
        {
            LogStartingFetchVideos(Logger, null);
            var videos = await RaindropAPI.GetVideosAsync(CancellationToken.None).ConfigureAwait(false);
            _videoItems = videos.ToList();
        }
        catch (Exception ex)
        {
            LogExceptionFetchingVideos(Logger, ex);
            _errorMessage = $"Exception fetching videos: {ex.Message}";
        }

        StateHasChanged();
    }

    private async Task StopShimmerAsync(string elementId)
    {
        try
        {
            await Js.InvokeVoidAsync("eval", $"document.getElementById('{elementId}')?.classList.add('loaded')").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogShimmerError(Logger, elementId, ex);
        }
    }
}