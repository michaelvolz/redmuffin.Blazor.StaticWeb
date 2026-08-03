using System.Text.Json;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common;
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
    public async Task<Result<IReadOnlyList<RaindropItem>>> GetVideosAsync(CancellationToken cancellationToken = default)
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
                return Result.Failure<IReadOnlyList<RaindropItem>>("The Raindrop endpoint returned an error response.");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                LogEmptyAPIResponse(_logger, "GetVideosAsync");
                return Result.Success<IReadOnlyList<RaindropItem>>([]);
            }

            var videos = await DeserializeWithFallbackAsync<List<RaindropItem>>(jsonContent, "videos API response").ConfigureAwait(false);

            if (videos is null)
                return Result.Failure<IReadOnlyList<RaindropItem>>("The Raindrop response could not be processed.");

            LogVideosLoaded(_logger, videos.Count);
            return Result.Success<IReadOnlyList<RaindropItem>>(videos);
        }
        catch (HttpRequestException ex)
        {
            LogAPIRequestError(_logger, ex, "GetVideosAsync");
            return Result.Failure<IReadOnlyList<RaindropItem>>("The Raindrop endpoint did not return a response.");
        }
        catch (OperationCanceledException ex)
        {
            LogOperationCancelled(_logger, ex, "GetVideosAsync");
            throw;
        }
        catch (JsonException ex)
        {
            LogJsonParseError(_logger, ex, "GetVideosAsync");
            return Result.Failure<IReadOnlyList<RaindropItem>>("The Raindrop response could not be processed.");
        }
        catch (Exception ex)
        {
            LogUnexpectedError(_logger, ex, "GetVideosAsync");
            return Result.Failure<IReadOnlyList<RaindropItem>>("An unexpected error occurred while retrieving videos.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RaindropItem>>> GetArticlesAsync(CancellationToken cancellationToken = default)
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
                return Result.Failure<IReadOnlyList<RaindropItem>>("The Raindrop endpoint returned an error response.");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                LogEmptyAPIResponse(_logger, "GetArticlesAsync");
                return Result.Success<IReadOnlyList<RaindropItem>>([]);
            }

            var articles = await DeserializeWithFallbackAsync<List<RaindropItem>>(jsonContent, "articles API response")
                .ConfigureAwait(false);

            if (articles is null)
                return Result.Failure<IReadOnlyList<RaindropItem>>("The Raindrop response could not be processed.");

            LogArticlesLoaded(_logger, articles.Count);
            return Result.Success<IReadOnlyList<RaindropItem>>(articles);
        }
        catch (HttpRequestException ex)
        {
            LogAPIRequestError(_logger, ex, "GetArticlesAsync");
            return Result.Failure<IReadOnlyList<RaindropItem>>("The Raindrop endpoint did not return a response.");
        }
        catch (OperationCanceledException ex)
        {
            LogOperationCancelled(_logger, ex, "GetArticlesAsync");
            throw;
        }
        catch (JsonException ex)
        {
            LogJsonParseError(_logger, ex, "GetArticlesAsync");
            return Result.Failure<IReadOnlyList<RaindropItem>>("The Raindrop response could not be processed.");
        }
        catch (Exception ex)
        {
            LogUnexpectedError(_logger, ex, "GetArticlesAsync");
            return Result.Failure<IReadOnlyList<RaindropItem>>("An unexpected error occurred while retrieving articles.");
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
