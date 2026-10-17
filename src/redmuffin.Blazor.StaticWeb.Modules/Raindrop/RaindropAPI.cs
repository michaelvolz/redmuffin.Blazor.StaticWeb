using System.Text.Json;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop;

/// <summary>
///     Real HTTP implementation of IRaindropAPI against Azure Functions endpoints.
/// </summary>
internal sealed partial class RaindropAPI(
    IHttpClientFactory httpClientFactory,
    ILogger<RaindropAPI> logger
) : IRaindropAPI, IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory =
        httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly ILogger<RaindropAPI> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));
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
    public async Task<Result<IReadOnlyList<RaindropItem>>> GetVideosAsync(
        CancellationToken cancellationToken = default
    )
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            return await FetchVideoListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            LogAPIRequestError(_logger, ex, "GetVideosAsync");
            return Result.Failure<IReadOnlyList<RaindropItem>>(
                "The Raindrop endpoint did not return a response."
            );
        }
        catch (OperationCanceledException ex)
        {
            LogOperationCancelled(_logger, ex, "GetVideosAsync");
            throw;
        }
        catch (JsonException ex)
        {
            LogJsonParseError(_logger, ex, "GetVideosAsync");
            return Result.Failure<IReadOnlyList<RaindropItem>>(
                "The Raindrop response could not be processed."
            );
        }
        catch (Exception ex)
        {
            LogUnexpectedError(_logger, ex, "GetVideosAsync");
            return Result.Failure<IReadOnlyList<RaindropItem>>(
                "An unexpected error occurred while retrieving videos."
            );
        }
    }

    private async Task<Result<IReadOnlyList<RaindropItem>>> FetchVideoListAsync(
        CancellationToken cancellationToken
    )
    {
        LogCallingVideosAPI(_logger);

        using var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient
            .GetAsync("/api/RaindropListVideos", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            LogAPICallFailed(
                _logger,
                "GetVideosAsync",
                (int)response.StatusCode,
                response.ReasonPhrase ?? "Unknown error"
            );
            return Result.Failure<IReadOnlyList<RaindropItem>>(
                "The Raindrop endpoint returned an error response."
            );
        }

        var jsonContent = await response
            .Content.ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            LogEmptyAPIResponse(_logger, "GetVideosAsync");
            return Result.Success<IReadOnlyList<RaindropItem>>([]);
        }

        var videos = await RaindropJsonDeserializer
            .DeserializeWithFallbackAsync<List<RaindropItem>>(
                jsonContent,
                _logger,
                "videos API response"
            )
            .ConfigureAwait(false);

        if (videos is null)
            return Result.Failure<IReadOnlyList<RaindropItem>>(
                "The Raindrop response could not be processed."
            );

        LogVideosLoaded(_logger, videos.Count);
        return Result.Success<IReadOnlyList<RaindropItem>>(videos);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RaindropItem>>> GetArticlesAsync(
        CancellationToken cancellationToken = default
    )
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            return await FetchArticleListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            LogAPIRequestError(_logger, ex, "GetArticlesAsync");
            return Result.Failure<IReadOnlyList<RaindropItem>>(
                "The Raindrop endpoint did not return a response."
            );
        }
        catch (OperationCanceledException ex)
        {
            LogOperationCancelled(_logger, ex, "GetArticlesAsync");
            throw;
        }
        catch (JsonException ex)
        {
            LogJsonParseError(_logger, ex, "GetArticlesAsync");
            return Result.Failure<IReadOnlyList<RaindropItem>>(
                "The Raindrop response could not be processed."
            );
        }
        catch (Exception ex)
        {
            LogUnexpectedError(_logger, ex, "GetArticlesAsync");
            return Result.Failure<IReadOnlyList<RaindropItem>>(
                "An unexpected error occurred while retrieving articles."
            );
        }
    }

    private async Task<Result<IReadOnlyList<RaindropItem>>> FetchArticleListAsync(
        CancellationToken cancellationToken
    )
    {
        LogCallingArticlesAPI(_logger);

        using var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient
            .GetAsync("/api/RaindropListArticles", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            LogAPICallFailed(
                _logger,
                "GetArticlesAsync",
                (int)response.StatusCode,
                response.ReasonPhrase ?? "Unknown error"
            );
            return Result.Failure<IReadOnlyList<RaindropItem>>(
                "The Raindrop endpoint returned an error response."
            );
        }

        var jsonContent = await response
            .Content.ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            LogEmptyAPIResponse(_logger, "GetArticlesAsync");
            return Result.Success<IReadOnlyList<RaindropItem>>([]);
        }

        var articles = await RaindropJsonDeserializer
            .DeserializeWithFallbackAsync<List<RaindropItem>>(
                jsonContent,
                _logger,
                "articles API response"
            )
            .ConfigureAwait(false);

        if (articles is null)
            return Result.Failure<IReadOnlyList<RaindropItem>>(
                "The Raindrop response could not be processed."
            );

        LogArticlesLoaded(_logger, articles.Count);
        return Result.Success<IReadOnlyList<RaindropItem>>(articles);
    }
}
