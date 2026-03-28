using Bunit;

namespace redmuffin.Blazor.StaticWeb.Tests.NewTests.Features.Cache.Components;

/// <summary>
///     Helper methods and infrastructure for RefreshBadgeTests.
/// </summary>
[Category("Feature:Cache")]
public partial class RefreshBadgeTests
{
    /// <summary>
    ///     Creates a new test scope with standard configuration.
    /// </summary>
    /// <returns>A configured test scope ready for component testing.</returns>
    private static TestScope CreateTestScope()
    {
        return new TestScope().WithStandardServices();
    }

    /// <summary>
    ///     Test scope that encapsulates all test resources with automatic disposal.
    ///     Uses C# 13 primary constructor pattern for clean, professional resource management.
    /// </summary>
    public sealed class TestScope : IDisposable
    {
        /// <summary>
        ///     Gets the bUnit test context for component rendering.
        /// </summary>
        public BunitContext Context { get; } = new();

        /// <summary>
        ///     Configures the test scope with standard services for component testing.
        /// </summary>
        /// <returns>The configured test scope for method chaining.</returns>
        public TestScope WithStandardServices()
        {
            // Add any required services for RefreshBadge component testing
            // Currently RefreshBadge doesn't require additional services
            return this;
        }

        /// <summary>
        ///     Disposes of test resources.
        /// </summary>
        public void Dispose()
        {
            Context?.Dispose();
        }
    }
}