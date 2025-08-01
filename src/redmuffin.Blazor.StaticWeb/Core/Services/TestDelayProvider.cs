using redmuffin.Blazor.StaticWeb.Core.Abstractions;

namespace redmuffin.Blazor.StaticWeb.Core.Services;

/// <summary>
///     Test implementation of IDelayProvider that provides no delays for fast test execution.
/// </summary>
public sealed class TestDelayProvider : IDelayProvider
{
    /// <inheritdoc />
    public Task DelayAsync(int milliseconds)
    {
        // No delay in test scenarios for optimal performance
        return Task.CompletedTask;
    }
}