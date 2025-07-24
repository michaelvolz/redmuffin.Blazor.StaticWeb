namespace redmuffin.Blazor.StaticWeb.Common.Abstractions;

/// <summary>
///     Provides delay functionality with configurable implementations for production and testing scenarios.
/// </summary>
public interface IDelayProvider
{
    /// <summary>
    ///     Asynchronously delays execution for the specified number of milliseconds.
    /// </summary>
    /// <param name="milliseconds">The number of milliseconds to delay.</param>
    /// <returns>A task representing the delay operation.</returns>
    Task DelayAsync(int milliseconds);
}