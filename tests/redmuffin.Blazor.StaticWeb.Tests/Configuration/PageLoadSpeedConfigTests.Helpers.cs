using redmuffin.Blazor.StaticWeb.Configuration;

namespace redmuffin.Blazor.StaticWeb.Tests.Configuration;

/// <summary>
///     Helper classes and methods for PageLoadSpeedConfigTests.
/// </summary>
[Category("Feature:Configuration")]
public sealed partial class PageLoadSpeedConfigTests
{
    private static readonly SemaphoreSlim ConfigurationGate = new(1, 1);

    private static async Task<ConfigurationScope> EnterExclusiveScopeAsync()
    {
        await ConfigurationGate.WaitAsync().ConfigureAwait(false);
        return new ConfigurationScope(
            PageLoadSpeedConfig.IsEnabled,
            PageLoadSpeedConfig.EnableOnLocalhost,
            PageLoadSpeedConfig.AutoLoadDelayMs,
            PageLoadSpeedConfig.JsInteropTimeoutSeconds);
    }

    public sealed class ConfigurationScope : IDisposable
    {
        private readonly bool _isEnabled;
        private readonly bool _enableOnLocalhost;
        private readonly int _autoLoadDelayMs;
        private readonly int _jsInteropTimeoutSeconds;
        private bool _disposed;

        public ConfigurationScope(bool isEnabled, bool enableOnLocalhost, int autoLoadDelayMs, int jsInteropTimeoutSeconds)
        {
            _isEnabled = isEnabled;
            _enableOnLocalhost = enableOnLocalhost;
            _autoLoadDelayMs = autoLoadDelayMs;
            _jsInteropTimeoutSeconds = jsInteropTimeoutSeconds;
        }

        public void Dispose()
        {
            if (_disposed) return;

            PageLoadSpeedConfig.IsEnabled = _isEnabled;
            PageLoadSpeedConfig.EnableOnLocalhost = _enableOnLocalhost;
            PageLoadSpeedConfig.AutoLoadDelayMs = _autoLoadDelayMs;
            PageLoadSpeedConfig.JsInteropTimeoutSeconds = _jsInteropTimeoutSeconds;
            _disposed = true;
            ConfigurationGate.Release();
        }
    }
}
