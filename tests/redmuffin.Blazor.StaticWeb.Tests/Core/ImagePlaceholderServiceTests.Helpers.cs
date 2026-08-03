using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Common.ImagePlaceholder;
using redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services;

namespace redmuffin.Blazor.StaticWeb.Tests.Core;

[Category("Feature:Core")]
public partial class ImagePlaceholderServiceTests
{
    /// <summary>
    ///     Creates a test scope with all required services for ImagePlaceholderService testing.
    /// </summary>
    private static TestScope CreateTestScope()
    {
        return new TestScope().WithImagePlaceholderServices();
    }

    /// <summary>
    ///     Creates a test RaindropItem with configurable properties.
    /// </summary>
    private static RaindropItem CreateTestItem(int id = 123, string? cover = "https://example.com/image.jpg")
    {
        return new RaindropItem
        {
            Id = id,
            Link = $"https://example.com/test-{id}",
            Cover = cover ?? string.Empty,
            Title = $"Test Item {id}",
            Excerpt = $"Test excerpt for item {id}"
        };
    }

    /// <summary>
    ///     Creates a cache dictionary with a failed item entry.
    /// </summary>
    private static Dictionary<string, string> CreateCacheWithFailedItem(string itemLink)
    {
        return new Dictionary<string, string>
        {
            { itemLink, "FAILED" }
        };
    }

    /// <summary>
    ///     Creates a cache dictionary with a valid cached URL.
    /// </summary>
    private static Dictionary<string, string> CreateCacheWithValidItem(string itemLink, string cachedUrl)
    {
        return new Dictionary<string, string>
        {
            { itemLink, cachedUrl }
        };
    }

    /// <summary>
    ///     Asserts that the result is a valid SVG data URL.
    /// </summary>
    private static async Task AssertIsSvgDataUrl(string result)
    {
        await Assert.That(result).IsNotNull();
        await Assert.That(result.StartsWith("data:image/svg+xml;base64,")).IsTrue();
        await Assert.That(result.Length > 50).IsTrue();
    }

    /// <summary>
    ///     Test scope for ImagePlaceholderService tests with proper service registration.
    /// </summary>
    public sealed class TestScope : IDisposable
    {
        private readonly ServiceCollection _services = new();
        public ServiceProvider ServiceProvider { get; private set; } = default!;

        /// <summary>
        ///     Configures the test scope with ImagePlaceholder services and dependencies.
        /// </summary>
        public TestScope WithImagePlaceholderServices()
        {
            // Register core services
            _services.AddSingleton<ILogger<ImagePlaceholderService>>(new Logger_Spy<ImagePlaceholderService>());
            _services.AddSingleton<IJSRuntime>(new JSRuntime_Stub());

            // Register ImagePlaceholder services
            _services.AddSingleton<IImagePlaceholderService, ImagePlaceholderService>();
            _services.AddSingleton<ImagePlaceholderService>();

            ServiceProvider = _services.BuildServiceProvider();
            return this;
        }

        public void Dispose()
        {
            ServiceProvider?.Dispose();
        }
    }

    /// <summary>
    ///     Test logger implementation for capturing log entries.
    /// </summary>
    public class Logger_Spy<T> : ILogger<T>
    {
        public List<LogEntry> LogEntries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return new NoOpDisposable();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            LogEntries.Add(new LogEntry(logLevel, eventId, message, exception));
        }
    }

    /// <summary>
    ///     Test JSRuntime implementation for testing JavaScript interop.
    /// </summary>
    public class JSRuntime_Stub : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return ValueTask.FromResult(default(TValue)!);
        }
    }

    /// <summary>
    ///     No-op disposable for test scopes.
    /// </summary>
    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    /// <summary>
    ///     Log entry record for test logging.
    /// </summary>
    public record LogEntry(LogLevel Level, EventId EventId, string Message, Exception? Exception);
}