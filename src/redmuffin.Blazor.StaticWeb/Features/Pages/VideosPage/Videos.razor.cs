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
            var response = await Http.GetAsync("/api/RaindropListVideos").ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                LogRawJsonResponse(Logger, json, null);

                try
                {
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
                    _errorMessage = "Error deserializing JSON: " + jsonEx.Message;
                    return;
                }
            }
            else
            {
                _errorMessage = $"Error fetching videos: {response.StatusCode} - {await response.Content.ReadAsStringAsync().ConfigureAwait(false)}";
            }
        }
        catch (Exception ex)
        {
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