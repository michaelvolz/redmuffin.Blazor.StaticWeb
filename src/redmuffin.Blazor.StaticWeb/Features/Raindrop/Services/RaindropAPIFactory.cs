using Microsoft.AspNetCore.Components;

namespace redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;

/// <summary>
///     Factory implementation for creating appropriate IRaindropAPI instances based on the current environment.
///     Uses NavigationManager.BaseUri to detect localhost:5233 (dummy data) vs localhost:4280 (real API).
/// </summary>
public sealed partial class RaindropAPIFactory : IRaindropAPIFactory
{
    private readonly NavigationManager _navigationManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RaindropAPIFactory> _logger;

    /// <summary>
    ///     Initializes a new instance of the RaindropAPIFactory class.
    /// </summary>
    /// <param name="navigationManager">Navigation manager for environment detection.</param>
    /// <param name="serviceProvider">Service provider for dependency resolution.</param>
    /// <param name="logger">Logger for factory operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public RaindropAPIFactory(
        NavigationManager navigationManager,
        IServiceProvider serviceProvider,
        ILogger<RaindropAPIFactory> logger)
    {
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IRaindropAPI CreateRaindropAPI()
    {
        try
        {
            var shouldUseDummy = ShouldUseDummyData();

            if (shouldUseDummy)
            {
                LogCreatingDummyAPI(_logger, _navigationManager.BaseUri);
                return _serviceProvider.GetRequiredService<DummyRaindropAPI>();
            }

            LogCreatingRealAPI(_logger, _navigationManager.BaseUri);
            return _serviceProvider.GetRequiredService<RaindropAPI>();
        }
        catch (Exception ex)
        {
            LogFactoryError(_logger, ex, _navigationManager.BaseUri);
            throw new InvalidOperationException(
                $"Failed to create IRaindropAPI instance for base URI: {_navigationManager.BaseUri}", ex);
        }
    }

    /// <inheritdoc />
    public bool ShouldUseDummyData()
    {
        try
        {
            var baseUri = _navigationManager.BaseUri;

            if (string.IsNullOrWhiteSpace(baseUri))
            {
                LogInvalidBaseUri(_logger, baseUri);
                throw new InvalidOperationException("Base URI cannot be null or empty.");
            }

            // Check for localhost:5233 (dummy data environment)
            var isDummyEnvironment = baseUri.Contains("localhost:5233", StringComparison.OrdinalIgnoreCase);

            LogEnvironmentDetection(_logger, baseUri, isDummyEnvironment);
            return isDummyEnvironment;
        }
        catch (Exception ex) when (!(ex is ArgumentException))
        {
            LogEnvironmentDetectionError(_logger, ex, _navigationManager.BaseUri);
            throw new InvalidOperationException(
                $"Failed to determine environment for base URI: {_navigationManager.BaseUri}", ex);
        }
    }
}