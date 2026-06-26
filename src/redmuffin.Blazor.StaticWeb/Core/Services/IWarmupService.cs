namespace redmuffin.Blazor.StaticWeb.Core.Services;

public interface IWarmupService
{
    /// <summary>
    ///     Attempts a best-effort HTTP call to wake cold Azure Functions.
    /// </summary>
    /// <returns><see langword="true" /> when a response was received; otherwise <see langword="false" />.</returns>
    Task<bool> TryWarmupAsync(CancellationToken cancellationToken = default);
}
