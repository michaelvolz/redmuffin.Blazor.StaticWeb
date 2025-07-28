using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Functions;

/// <summary>
///     Helper methods and TestScope implementation for ArticlesApiVerification_Tests.
///     Provides HttpClient and Configuration setup for API testing scenarios.
/// </summary>
public sealed partial class ArticlesApiVerification_Tests
{
    /// <summary>
    ///     Creates a configured TestScope with HttpClient and Configuration for API verification tests.
    /// </summary>
    /// <returns>A TestScope with configured dependencies.</returns>
    private static TestScope CreateTestScope()
    {
        var services = new ServiceCollection();

        // Add HttpClient services
        services.AddHttpClient();

        var serviceProvider = services.BuildServiceProvider();

        // Build configuration from local.settings.json and environment variables
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.settings.json", true)
            .AddEnvironmentVariables()
            .Build();

        return new TestScope(serviceProvider, configuration);
    }

    /// <summary>
    ///     TestScope for managing test dependencies including HttpClient and Configuration.
    ///     Provides configured services for API verification testing.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="configuration">Configuration instance for accessing test settings.</param>
    public sealed class TestScope(ServiceProvider serviceProvider, IConfiguration configuration) : IDisposable
    {
        public ServiceProvider ServiceProvider { get; } = serviceProvider;
        public IConfiguration Configuration { get; } = configuration;

        /// <summary>
        ///     Creates an HttpClient instance for API testing.
        /// </summary>
        /// <returns>A configured HttpClient instance.</returns>
        public static HttpClient CreateHttpClient()
        {
            return new HttpClient();
        }

        public void Dispose()
        {
            ServiceProvider?.Dispose();
        }
    }
}