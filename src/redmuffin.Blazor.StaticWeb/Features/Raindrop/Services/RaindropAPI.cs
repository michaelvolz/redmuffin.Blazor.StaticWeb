using System.Text.Json;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

/// <summary>
///     Real implementation of IRaindropAPI that makes HTTP calls to Azure Functions for RaindropIO API operations.
///     Routes all API calls through Azure Functions endpoints for production and localhost:4280 environments.
/// </summary>
public sealed partial class RaindropAPI(IHttpClientFactory httpClientFactory, ILogger<RaindropAPI> logger) : IRaindropAPI, IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly ILogger<RaindropAPI> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private bool _disposed;

    /// <summary>
    ///     Releases all resources used by the RaindropAPI.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            LogDisposing(_logger);
            _disposed = true;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RaindropItem>> GetVideosAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            LogCallingVideosAPI(_logger);

            using var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync("/api/RaindropListVideos", cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                LogAPICallFailed(_logger, "GetVideosAsync", (int)response.StatusCode, response.ReasonPhrase ?? "Unknown error");
                response.EnsureSuccessStatusCode();
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                LogEmptyAPIResponse(_logger, "GetVideosAsync");
                return new List<RaindropItem>();
            }

            var videos = await DeserializeWithFallbackAsync<List<RaindropItem>>(jsonContent, "videos API response").ConfigureAwait(false);

            if (videos == null) throw new InvalidOperationException("Failed to deserialize videos API response - all deserialization strategies failed.");

            LogVideosLoaded(_logger, videos.Count);
            return videos;
        }
        catch (HttpRequestException ex)
        {
            LogAPIRequestError(_logger, ex, "GetVideosAsync");
            throw;
        }
        catch (OperationCanceledException ex)
        {
            LogOperationCancelled(_logger, ex, "GetVideosAsync");
            throw;
        }
        catch (JsonException ex)
        {
            LogJsonParseError(_logger, ex, "GetVideosAsync");
            throw new InvalidOperationException("Failed to parse videos API response.", ex);
        }
        catch (Exception ex)
        {
            LogUnexpectedError(_logger, ex, "GetVideosAsync");
            throw new InvalidOperationException("An unexpected error occurred while retrieving videos.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RaindropItem>> GetArticlesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            LogCallingArticlesAPI(_logger);

            using var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync("/api/RaindropListArticles", cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                LogAPICallFailed(_logger, "GetArticlesAsync", (int)response.StatusCode, response.ReasonPhrase ?? "Unknown error");
                response.EnsureSuccessStatusCode();
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                LogEmptyAPIResponse(_logger, "GetArticlesAsync");
                return new List<RaindropItem>();
            }

            var articles = await DeserializeWithFallbackAsync<List<RaindropItem>>(jsonContent, "articles API response")
                .ConfigureAwait(false);

            if (articles == null) throw new InvalidOperationException("Failed to deserialize articles API response - all deserialization strategies failed.");

            LogArticlesLoaded(_logger, articles.Count);
            return articles;
        }
        catch (HttpRequestException ex)
        {
            LogAPIRequestError(_logger, ex, "GetArticlesAsync");
            throw;
        }
        catch (OperationCanceledException ex)
        {
            LogOperationCancelled(_logger, ex, "GetArticlesAsync");
            throw;
        }
        catch (JsonException ex)
        {
            LogJsonParseError(_logger, ex, "GetArticlesAsync");
            throw new InvalidOperationException("Failed to parse articles API response.", ex);
        }
        catch (Exception ex)
        {
            LogUnexpectedError(_logger, ex, "GetArticlesAsync");
            throw new InvalidOperationException("An unexpected error occurred while retrieving articles.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<string> GetHelloWorldAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(cancellationToken);

        try
        {
            LogCallingHelloWorldAPI(_logger);

            using var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync("/api/HelloWorld", cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                LogAPICallFailed(_logger, "GetHelloWorldAsync", (int)response.StatusCode, response.ReasonPhrase ?? "Unknown error");
                response.EnsureSuccessStatusCode();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(content))
            {
                LogEmptyAPIResponse(_logger, "GetHelloWorldAsync");
                return "Hello World (empty response)";
            }

            LogHelloWorldAPISuccess(_logger);
            return content;
        }
        catch (HttpRequestException ex)
        {
            LogHelloWorldAPIError(_logger, ex);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            LogOperationCancelled(_logger, ex, "GetHelloWorldAsync");
            throw;
        }
        catch (Exception ex)
        {
            LogUnexpectedError(_logger, ex, "GetHelloWorldAsync");
            throw new InvalidOperationException("An unexpected error occurred while retrieving Hello World message.", ex);
        }
    }

    /// <summary>
    ///     Deserializes JSON content with multiple fallback strategies for robust error handling.
    ///     Uses DefaultOptions first, then LenientOptions for malformed JSON, and finally StrictOptions as last resort.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="jsonContent">The JSON content to deserialize.</param>
    /// <param name="source">The source description for logging purposes.</param>
    /// <returns>The deserialized object or null if all strategies fail.</returns>
    private Task<T?> DeserializeWithFallbackAsync<T>(string jsonContent, string source) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonContent);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        // Strategy 1: Try with DefaultOptions (most common case)
        try
        {
            LogAttemptingDeserialization(_logger, source, "DefaultOptions");
            var result = JsonSerializer.Deserialize<T>(jsonContent, RaindropJsonSerializerContext.DefaultOptions);
            if (result != null)
            {
                LogDeserializationSuccess(_logger, source, "DefaultOptions");
                return Task.FromResult<T?>(result);
            }
        }
        catch (JsonException ex)
        {
            LogDeserializationAttemptFailed(_logger, ex, source, "DefaultOptions");
        }

        // Strategy 2: Try with LenientOptions for malformed JSON
        try
        {
            LogAttemptingDeserialization(_logger, source, "LenientOptions");
            var result = JsonSerializer.Deserialize<T>(jsonContent, RaindropJsonSerializerContext.LenientOptions);
            if (result != null)
            {
                LogDeserializationSuccess(_logger, source, "LenientOptions");
                return Task.FromResult<T?>(result);
            }
        }
        catch (JsonException ex)
        {
            LogDeserializationAttemptFailed(_logger, ex, source, "LenientOptions");
        }

        // Strategy 3: Try with StrictOptions as last resort
        try
        {
            LogAttemptingDeserialization(_logger, source, "StrictOptions");
            var result = JsonSerializer.Deserialize<T>(jsonContent, RaindropJsonSerializerContext.StrictOptions);
            if (result != null)
            {
                LogDeserializationSuccess(_logger, source, "StrictOptions");
                return Task.FromResult<T?>(result);
            }
        }
        catch (JsonException ex)
        {
            LogDeserializationAttemptFailed(_logger, ex, source, "StrictOptions");
        }

        // All strategies failed
        LogAllDeserializationStrategiesFailed(_logger, source);
        return Task.FromResult<T?>(null);
    }
}