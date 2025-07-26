using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Abstractions;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.VideosPage;

public partial class Videos
{
    private readonly Dictionary<string, string> _imageUrlCache = new(StringComparer.Ordinal);
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

    [Inject]
    private IImagePlaceholderService ImagePlaceholderService { get; set; } = null!;

    [Inject]
    private IImageValidationCacheService ImageValidationCacheService { get; set; } = null!;

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
        ArgumentNullException.ThrowIfNull(ImagePlaceholderService);
        ArgumentNullException.ThrowIfNull(ImageValidationCacheService);

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
            var videos = await RaindropAPI.GetVideosAsync(CancellationToken.None).ConfigureAwait(false);
            _videoItems = videos.ToList();

            // Populate image cache for videos
            if (_videoItems.Count > 0)
            {
                await ImageValidationCacheService.PopulateImageUrlCacheAsync(
                    _videoItems,
                    _imageUrlCache,
                    () => InvokeAsync(StateHasChanged),
                    CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Exception fetching videos: {ex.Message}";
        }

        StateHasChanged();
    }

    private string GetDefaultPlaceholder()
    {
        return ImagePlaceholderService.GetDefaultPlaceholder();
    }

    private string GetImageUrl(RaindropItem video)
    {
        return ImagePlaceholderService.GetImageUrl(video, _imageUrlCache);
    }

    private Task HandleImageLoadAsync(string elementId, string videoLink, bool loadSuccess)
    {
        return ImagePlaceholderService.HandleImageLoadAsync(
            elementId,
            videoLink,
            loadSuccess,
            _imageUrlCache,
            Js,
            () => InvokeAsync(StateHasChanged));
    }

    private bool HasFallbackPlaceholder(RaindropItem video)
    {
        return ImagePlaceholderService.HasFallbackPlaceholder(video, _imageUrlCache);
    }

    private string GetFallbackReason(RaindropItem video)
    {
        return ImagePlaceholderService.GetFallbackReason(video, _imageUrlCache);
    }
}