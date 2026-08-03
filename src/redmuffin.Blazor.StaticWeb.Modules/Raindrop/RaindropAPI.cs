using System.Text.Json;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop;

/// <summary>
///     Real HTTP implementation of IRaindropAPI against Azure Functions endpoints.
/// </summary>
internal sealed partial class RaindropAPI(IHttpClientFactory httpClientFactory, ILogger<RaindropAPI> logger) : IRaindropAPI, IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly ILogger<RaindropAPI> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private bool _disposed;

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

    private Task<T?> DeserializeWithFallbackAsync<T>(string jsonContent, string source) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonContent);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

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

        LogAllDeserializationStrategiesFailed(_logger, source);
        return Task.FromResult<T?>(null);
    }
}
