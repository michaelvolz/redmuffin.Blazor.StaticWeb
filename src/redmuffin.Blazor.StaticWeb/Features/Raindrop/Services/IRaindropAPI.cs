using redmuffin.Blazor.StaticWeb.Common.Raindrop;

namespace redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

/// <summary>
/// Provides abstraction for RaindropIO API operations to support both real API calls and dummy data for local development.
/// </summary>
public interface IRaindropAPI
{
    /// <summary>
    /// Retrieves a collection of video items from the RaindropIO API.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of video RaindropItems.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails due to network or server issues.</exception>
    /// <exception cref="TaskCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the API response cannot be processed or is in an unexpected format.</exception>
    Task<IEnumerable<RaindropItem>> GetVideosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a collection of article items from the RaindropIO API.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of article RaindropItems.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails due to network or server issues.</exception>
    /// <exception cref="TaskCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the API response cannot be processed or is in an unexpected format.</exception>
    Task<IEnumerable<RaindropItem>> GetArticlesAsync(CancellationToken cancellationToken = default);
}