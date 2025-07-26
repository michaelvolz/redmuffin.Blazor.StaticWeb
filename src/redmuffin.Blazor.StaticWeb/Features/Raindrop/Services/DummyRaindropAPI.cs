using System.Text.Json;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

/// <summary>
///     Dummy implementation of IRaindropAPI that loads data from JSON files for local development and testing.
///     Uses static JSON files in the mockdata folder to simulate API responses.
///     Enhanced with robust JSON parsing using multiple serialization strategies for edge cases.
/// </summary>
public sealed partial class DummyRaindropAPI(IHttpClientFactory httpClientFactory, ILogger<DummyRaindropAPI> logger) : IRaindropAPI, IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly ILogger<DummyRaindropAPI> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private bool _disposed;

    /// <summary>
    ///     Releases all resources used by the DummyRaindropAPI.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        LogDisposing(_logger);
        _disposed = true;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RaindropItem>> GetVideosAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            LogLoadingVideos(_logger);

            var jsonContent = await LoadJsonFileAsync("mockdata/videos.json", cancellationToken).ConfigureAwait(false);
            var videos = await DeserializeWithFallbackAsync<List<RaindropItem>>(jsonContent, "videos.json", cancellationToken).ConfigureAwait(false);

            if (videos == null) throw new InvalidOperationException("Failed to deserialize videos JSON data - all deserialization strategies failed.");

            LogVideosLoaded(_logger, videos.Count);
            return videos;
        }
        catch (HttpRequestException ex)
        {
            LogFileLoadError(_logger, ex, "videos.json");
            // Return empty collection for missing files (404) to support development scenarios
            return new List<RaindropItem>();
        }
        catch (JsonException ex)
        {
            LogJsonParseError(_logger, ex, "videos.json");
            throw new InvalidOperationException("Failed to parse videos JSON data.", ex);
        }
        catch (OperationCanceledException ex)
        {
            LogOperationCancelled(_logger, ex, "GetVideosAsync");
            throw;
        }
        catch (Exception ex)
        {
            LogUnexpectedError(_logger, ex, "GetVideosAsync");
            throw new InvalidOperationException("An unexpected error occurred while loading videos.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RaindropItem>> GetArticlesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            LogLoadingArticles(_logger);

            var jsonContent = await LoadJsonFileAsync("mockdata/articles.json", cancellationToken).ConfigureAwait(false);
            var articles = await DeserializeWithFallbackAsync<List<RaindropItem>>(jsonContent, "articles.json", cancellationToken).ConfigureAwait(false);

            if (articles == null) throw new InvalidOperationException("Failed to deserialize articles JSON data - all deserialization strategies failed.");

            LogArticlesLoaded(_logger, articles.Count);
            return articles;
        }
        catch (HttpRequestException ex)
        {
            LogFileLoadError(_logger, ex, "articles.json");
            // Return empty collection for missing files (404) to support development scenarios
            return new List<RaindropItem>();
        }
        catch (JsonException ex)
        {
            LogJsonParseError(_logger, ex, "articles.json");
            throw new InvalidOperationException("Failed to parse articles JSON data.", ex);
        }
        catch (OperationCanceledException ex)
        {
            LogOperationCancelled(_logger, ex, "GetArticlesAsync");
            throw;
        }
        catch (Exception ex)
        {
            LogUnexpectedError(_logger, ex, "GetArticlesAsync");
            throw new InvalidOperationException("An unexpected error occurred while loading articles.", ex);
        }
    }

    /// <summary>
    ///     Loads JSON content from the specified file path.
    /// </summary>
    /// <param name="relativeUrlPath">The relative path to the JSON file.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The JSON content as a string.</returns>
    /// <exception cref="HttpRequestException">Thrown when the file cannot be loaded.</exception>
    /// <exception cref="TaskCanceledException">Thrown when the operation is cancelled.</exception>
    private async Task<string> LoadJsonFileAsync(string relativeUrlPath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeUrlPath);

        LogLoadingFile(_logger, relativeUrlPath);

        using var httpClient = _httpClientFactory.CreateClient("DefaultClient");
        var response = await httpClient.GetAsync(relativeUrlPath, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(content)) throw new InvalidOperationException($"JSON file '{relativeUrlPath}' is empty or contains only whitespace.");

        LogFileLoaded(_logger, relativeUrlPath, content.Length);
        return content;
    }

    /// <summary>
    ///     Deserializes JSON content with multiple fallback strategies for robust error handling.
    ///     Uses DefaultOptions first, then LenientOptions for malformed JSON, and finally StrictOptions as last resort.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="jsonContent">The JSON content to deserialize.</param>
    /// <param name="fileName">The file name for logging purposes.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The deserialized object or null if all strategies fail.</returns>
    private Task<T?> DeserializeWithFallbackAsync<T>(string jsonContent, string fileName, CancellationToken cancellationToken) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonContent);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        // Strategy 1: Try with DefaultOptions (most common case)
        try
        {
            LogAttemptingDeserialization(_logger, fileName, "DefaultOptions");
            var result = JsonSerializer.Deserialize<T>(jsonContent, RaindropJsonSerializerContext.DefaultOptions);
            if (result != null)
            {
                LogDeserializationSuccess(_logger, fileName, "DefaultOptions");
                return Task.FromResult<T?>(result);
            }
        }
        catch (JsonException ex)
        {
            LogDeserializationAttemptFailed(_logger, ex, fileName, "DefaultOptions");
        }

        // Strategy 2: Try with LenientOptions for malformed JSON
        try
        {
            LogAttemptingDeserialization(_logger, fileName, "LenientOptions");
            var result = JsonSerializer.Deserialize<T>(jsonContent, RaindropJsonSerializerContext.LenientOptions);
            if (result != null)
            {
                LogDeserializationSuccess(_logger, fileName, "LenientOptions");
                return Task.FromResult<T?>(result);
            }
        }
        catch (JsonException ex)
        {
            LogDeserializationAttemptFailed(_logger, ex, fileName, "LenientOptions");
        }

        // Strategy 3: Try with StrictOptions as last resort
        try
        {
            LogAttemptingDeserialization(_logger, fileName, "StrictOptions");
            var result = JsonSerializer.Deserialize<T>(jsonContent, RaindropJsonSerializerContext.StrictOptions);
            if (result != null)
            {
                LogDeserializationSuccess(_logger, fileName, "StrictOptions");
                return Task.FromResult<T?>(result);
            }
        }
        catch (JsonException ex)
        {
            LogDeserializationAttemptFailed(_logger, ex, fileName, "StrictOptions");
        }

        // All strategies failed
        LogAllDeserializationStrategiesFailed(_logger, fileName);
        return Task.FromResult<T?>(null);
    }

    /// <summary>
    ///     Validates and sanitizes JSON content before deserialization.
    ///     Handles common edge cases like empty arrays, null values, and malformed structures.
    /// </summary>
    /// <param name="jsonContent">The raw JSON content.</param>
    /// <param name="fileName">The file name for logging purposes.</param>
    /// <returns>Sanitized JSON content ready for deserialization.</returns>
    private string SanitizeJsonContent(string jsonContent, string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonContent);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var trimmed = jsonContent.Trim();

        // Handle empty or whitespace-only content
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            LogJsonContentEmpty(_logger, fileName);
            return "[]";
        }

        // Handle null content
        if (string.Equals(trimmed, "null", StringComparison.OrdinalIgnoreCase))
        {
            LogJsonContentNull(_logger, fileName);
            return "[]";
        }

        // Handle malformed array start/end
        if (!trimmed.StartsWith('[') && !trimmed.StartsWith('{'))
        {
            LogJsonContentMalformed(_logger, fileName, "Missing opening bracket/brace");
            return $"[{trimmed}]";
        }

        return trimmed;
    }
}