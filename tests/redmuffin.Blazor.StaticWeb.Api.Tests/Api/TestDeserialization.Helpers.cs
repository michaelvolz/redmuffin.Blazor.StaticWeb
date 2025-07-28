using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace redmuffin.Blazor.StaticWeb.Api.Tests.Api;

/// <summary>
///     Helper methods and TestScope implementation for TestDeserialization tests.
///     Provides configured JSON serialization options and test infrastructure.
/// </summary>
public sealed partial class TestDeserialization
{
    /// <summary>
    ///     Creates a configured TestScope with JSON serialization options for deserialization tests.
    /// </summary>
    /// <returns>A TestScope with configured JsonSerializerOptions.</returns>
    private static TestScope CreateTestScope()
    {
        var services = new ServiceCollection();

        var serviceProvider = services.BuildServiceProvider();

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return new TestScope(serviceProvider, jsonSerializerOptions);
    }

    /// <summary>
    ///     TestScope for managing test dependencies and configuration.
    ///     Provides JsonSerializerOptions with case-insensitive property matching.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="jsonSerializerOptions">Configured JSON serializer options.</param>
    public sealed class TestScope(ServiceProvider serviceProvider, JsonSerializerOptions jsonSerializerOptions) : IDisposable
    {
        public ServiceProvider ServiceProvider { get; } = serviceProvider;
        public JsonSerializerOptions JsonSerializerOptions { get; } = jsonSerializerOptions;

        public void Dispose()
        {
            ServiceProvider?.Dispose();
        }
    }
}