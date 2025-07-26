namespace redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

/// <summary>
/// Provides factory abstraction for creating appropriate IRaindropAPI instances based on the current environment.
/// Supports environment detection to determine whether to use real API calls or dummy data for local development.
/// </summary>
public interface IRaindropAPIFactory
{
    /// <summary>
    /// Creates an appropriate IRaindropAPI instance based on the current environment.
    /// Returns DummyRaindropAPI for localhost:5233 (local development) and RaindropAPI for localhost:4280 (real API).
    /// </summary>
    /// <returns>An IRaindropAPI instance configured for the current environment.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the environment cannot be determined or is unsupported.</exception>
    /// <exception cref="ArgumentException">Thrown when required configuration values are missing or invalid.</exception>
    IRaindropAPI CreateRaindropAPI();

    /// <summary>
    /// Determines if the current environment should use dummy data based on the base URI.
    /// </summary>
    /// <returns>True if dummy data should be used (localhost:5233), false for real API (localhost:4280).</returns>
    bool ShouldUseDummyData();
}