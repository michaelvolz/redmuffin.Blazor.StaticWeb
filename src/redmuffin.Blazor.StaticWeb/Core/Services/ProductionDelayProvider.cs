using redmuffin.Blazor.StaticWeb.Core.Abstractions;

namespace redmuffin.Blazor.StaticWeb.Core.Services;

/// <summary>
///     Production implementation of IDelayProvider that provides real delays for user experience.
/// </summary>
public sealed class ProductionDelayProvider : IDelayProvider
{
    /// <inheritdoc />
    public Task DelayAsync(int milliseconds)
    {
        return Task.Delay(milliseconds);
    }
}