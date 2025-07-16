using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.VideosPage;

public partial class Videos
{
    // LoggerMessage delegates for better performance
    private static readonly Action<ILogger, string, Exception?> LogRawJsonResponse =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, nameof(LogRawJsonResponse)),
            "Raw JSON Response: {JsonResponse}");

    private static readonly Action<ILogger, string?, string?, string?, Exception> LogJsonDeserializationError =
        LoggerMessage.Define<string?, string?, string?>(LogLevel.Error, new EventId(2, nameof(LogJsonDeserializationError)),
            "JSON Deserialization Error. Path: {Path}, LineNumber: {LineNumber}, BytePositionInLine: {BytePositionInLine}");

    private static readonly Action<ILogger, string, Exception> LogShimmerError =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(3, nameof(LogShimmerError)),
            "Error stopping shimmer for element: {ElementId}");

    private static readonly Action<ILogger, Exception?> LogStartingFetchVideos =
        LoggerMessage.Define(LogLevel.Information, new EventId(4, nameof(LogStartingFetchVideos)),
            "Starting to fetch videos from /api/RaindropListVideos");

    private static readonly Action<ILogger, int, Exception?> LogResponseStatus =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(5, nameof(LogResponseStatus)),
            "Response status: {StatusCode}");

    private static readonly Action<ILogger, string, Exception?> LogResponseHeaders =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(6, nameof(LogResponseHeaders)),
            "Response headers: {Headers}");

    private static readonly Action<ILogger, int, Exception?> LogResponseContentLength =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(7, nameof(LogResponseContentLength)),
            "Response content length: {Length}");

    private static readonly Action<ILogger, string, Exception?> LogResponseContentPreview =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(8, nameof(LogResponseContentPreview)),
            "Response content preview: {Preview}");

    private static readonly Action<ILogger, Exception> LogExceptionFetchingVideos =
        LoggerMessage.Define(LogLevel.Error, new EventId(9, nameof(LogExceptionFetchingVideos)),
            "Exception occurred while fetching videos");

    private string? _errorMessage;
    private List<RaindropItem>? _videoItems;

    [Inject]
    private ILogger<Videos> Logger { get; set; } = null!;

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

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
            var response = await Http.GetAsync("/api/RaindropListVideos").ConfigureAwait(false);

            LogResponseStatus(Logger, (int)response.StatusCode, null);
            LogResponseHeaders(Logger, response.Headers.ToString(), null);

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            LogResponseContentLength(Logger, json.Length, null);
            LogResponseContentPreview(Logger, json.Length > 100 ? json.Substring(0, 100) : json, null);

            if (response.IsSuccessStatusCode)
            {
                LogRawJsonResponse(Logger, json, null);

                try
                {
                    // Check if the response starts with '<' which indicates HTML instead of JSON
                    if (json.TrimStart().StartsWith('<'))
                    {
                        _errorMessage = $"Received HTML response instead of JSON. Response: {json.Substring(0, Math.Min(500, json.Length))}";
                        return;
                    }

                    // Use JsonTypeInfo for deserialization to avoid trimming issues
                    _videoItems = JsonSerializer.Deserialize(json, RaindropJsonSerializerContext.Default.RaindropItemList);
                }
                catch (JsonException jsonEx)
                {
                    LogJsonDeserializationError(
                        Logger,
                        jsonEx.Path?.ToString(CultureInfo.InvariantCulture),
                        jsonEx.LineNumber?.ToString(CultureInfo.InvariantCulture),
                        jsonEx.BytePositionInLine?.ToString(CultureInfo.InvariantCulture),
                        jsonEx);
                    _errorMessage = $"Error deserializing JSON: {jsonEx.Message}. Response content: {json.Substring(0, Math.Min(500, json.Length))}";
                    return;
                }
            }
            else
            {
                _errorMessage = $"Error fetching videos: {response.StatusCode} - {json}";
            }
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

    protected override async Task OnInitializedAsync()
    {
        // Validate injected dependencies
        ArgumentNullException.ThrowIfNull(Http);
        ArgumentNullException.ThrowIfNull(Logger);
        ArgumentNullException.ThrowIfNull(Js);
        ArgumentNullException.ThrowIfNull(Navigation);

        // Load articles automatically when the page starts
        await FetchVideosAsync();
    }

    private static string DisplayTitle(RaindropItem video)
    {
        return string.IsNullOrEmpty(video.Title) ? "No Title Available" : video.Title;
    }

    private static string DisplayExcerpt(RaindropItem video)
    {
        return string.IsNullOrEmpty(video.Excerpt) ? "No Excerpt Available" :
            video.Excerpt.Length > 250 ? video.Excerpt.Substring(0, 250) + "..." : video.Excerpt;
    }
}