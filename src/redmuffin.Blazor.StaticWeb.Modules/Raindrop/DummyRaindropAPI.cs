using System.Text.Json;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop;

/// <summary>
///     Dummy IRaindropAPI that loads JSON from mockdata for local development (localhost:5233).
/// </summary>
internal sealed partial class DummyRaindropAPI(
    IHttpClientFactory httpClientFactory,
    ILogger<DummyRaindropAPI> logger
) : IRaindropAPI, IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory =
        httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly ILogger<DummyRaindropAPI> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
            return;

        LogDisposing(_logger);
        _disposed = true;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RaindropItem>>> GetVideosAsync(
        CancellationToken cancellationToken = default
    )
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            LogLoadingVideos(_logger);

            var jsonContent = await LoadJsonFileAsync("mockdata/videos.json", cancellationToken)
                .ConfigureAwait(false);
            var videos = await RaindropJsonDeserializer
                .DeserializeWithFallbackAsync<List<RaindropItem>>(
                    jsonContent,
                    _logger,
                    "videos.json"
                )
                .ConfigureAwait(false);

            if (videos is null)
                return Result.Failure<IReadOnlyList<RaindropItem>>(
                    "The Raindrop mock data could not be processed."
                );

            LogVideosLoaded(_logger, videos.Count);
            return Result.Success<IReadOnlyList<RaindropItem>>(videos);
        }
        catch (HttpRequestException ex)
        {
            LogFileLoadError(_logger, ex, "videos.json");
            return Result.Success<IReadOnlyList<RaindropItem>>([]);
        }
        catch (JsonException ex)
        {
            LogJsonParseError(_logger, ex, "videos.json");
            return Result.Failure<IReadOnlyList<RaindropItem>>(
                "The Raindrop mock data could not be processed."
            );
        }
        catch (OperationCanceledException ex)
        {
            LogOperationCancelled(_logger, ex, "GetVideosAsync");
            throw;
        }
        catch (Exception ex)
        {
            LogUnexpectedError(_logger, ex, "GetVideosAsync");
            return Result.Failure<IReadOnlyList<RaindropItem>>(
                "An unexpected error occurred while loading videos."
            );
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RaindropItem>>> GetArticlesAsync(
        CancellationToken cancellationToken = default
    )
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            LogLoadingArticles(_logger);

            var jsonContent = await LoadJsonFileAsync("mockdata/articles.json", cancellationToken)
                .ConfigureAwait(false);
            var articles = await RaindropJsonDeserializer
                .DeserializeWithFallbackAsync<List<RaindropItem>>(
                    jsonContent,
                    _logger,
                    "articles.json"
                )
                .ConfigureAwait(false);

            if (articles is null)
                return Result.Failure<IReadOnlyList<RaindropItem>>(
                    "The Raindrop mock data could not be processed."
                );

            LogArticlesLoaded(_logger, articles.Count);
            return Result.Success<IReadOnlyList<RaindropItem>>(articles);
        }
        catch (HttpRequestException ex)
        {
            LogFileLoadError(_logger, ex, "articles.json");
            return Result.Success<IReadOnlyList<RaindropItem>>([]);
        }
        catch (JsonException ex)
        {
            LogJsonParseError(_logger, ex, "articles.json");
            return Result.Failure<IReadOnlyList<RaindropItem>>(
                "The Raindrop mock data could not be processed."
            );
        }
        catch (OperationCanceledException ex)
        {
            LogOperationCancelled(_logger, ex, "GetArticlesAsync");
            throw;
        }
        catch (Exception ex)
        {
            LogUnexpectedError(_logger, ex, "GetArticlesAsync");
            return Result.Failure<IReadOnlyList<RaindropItem>>(
                "An unexpected error occurred while loading articles."
            );
        }
    }

    private async Task<string> LoadJsonFileAsync(
        string relativeUrlPath,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeUrlPath);

        LogLoadingFile(_logger, relativeUrlPath);

        using var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient
            .GetAsync(relativeUrlPath, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var content = await response
            .Content.ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException(
                $"JSON file '{relativeUrlPath}' is empty or contains only whitespace."
            );

        LogFileLoaded(_logger, relativeUrlPath, content.Length);
        return content;
    }
}
