using System.Text.Json;
using Blazored.LocalStorage;
using LightMock.Generator;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Features.Cache.Services;
using redmuffin.Blazor.StaticWeb.Features.Raindrop.Services;
using redmuffin.Blazor.StaticWeb.Features.RaindropItems.Models;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Cache.Services;

[Category("Feature:Cache")]
public partial class RaindropItemsCacheTests
{
    /// <summary>
    ///     Creates a performance test dataset with 1000 items for load testing.
    /// </summary>
    /// <returns>A list of test raindrop items for performance testing.</returns>
    public static List<RaindropItem> CreatePerformanceTestDataSet()
    {
        var items = new List<RaindropItem>();
        for (var i = 0; i < 1000; i++)
            items.Add(new RaindropItem
            {
                Id = i,
                Link = $"https://example.com/item/{i}",
                Title = $"Performance Test Item {i} with longer title text to make it more realistic",
                Excerpt =
                    $"This is a performance test excerpt for item {i}. It contains descriptive text that would normally be found in a real article or video description. This helps simulate real-world data sizes and compression scenarios for performance testing.",
                Cover = $"https://example.com/images/cover_{i}.jpg",
                Created = DateTime.UtcNow.AddDays(-i),
                Type = i % 2 == 0 ? "article" : "video"
            });
        return items;
    }

    // Factory methods for creating test scopes
    private static TestScope CreateTestScope()
    {
        return new TestScope();
    }

    /// <summary>
    ///     Creates test RaindropItem instances for testing cache operations.
    /// </summary>
    private static List<RaindropItem> CreateTestRaindropItems()
    {
        return
        [
            new RaindropItem
            {
                Id = 1,
                Link = "https://example.com/video1",
                Title = "Test Video 1",
                Excerpt = "Test excerpt 1",
                Cover = "https://example.com/cover1.jpg",
                Domain = "example.com",
                Created = DateTime.UtcNow.AddDays(-1),
                Type = "video"
            },
            new RaindropItem
            {
                Id = 2,
                Link = "https://example.com/video2",
                Title = "Test Video 2",
                Excerpt = "Test excerpt 2",
                Cover = "https://example.com/cover2.jpg",
                Domain = "example.com",
                Created = DateTime.UtcNow.AddDays(-2),
                Type = "video"
            }
        ];
    }

    /// <summary>
    ///     Creates test cache metadata that is not expired.
    /// </summary>
    private static RaindropCacheMetadata CreateTestMetadata()
    {
        return new RaindropCacheMetadata
        {
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            LastAccessedAt = DateTimeOffset.UtcNow.AddMinutes(-30),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(27), // 4 weeks - 1 day
            Version = "1.0",
            ItemCount = 2,
            CompressedSize = 1024,
            OriginalSize = 2048
        };
    }

    /// <summary>
    ///     Creates test cache metadata that is expired.
    /// </summary>
    private static RaindropCacheMetadata CreateExpiredTestMetadata()
    {
        return new RaindropCacheMetadata
        {
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            LastAccessedAt = DateTimeOffset.UtcNow.AddDays(-29),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1), // Expired yesterday
            Version = "1.0",
            ItemCount = 2,
            CompressedSize = 1024,
            OriginalSize = 2048
        };
    }

    /// <summary>
    ///     Creates a large dataset of test RaindropItem instances for compression testing.
    /// </summary>
    private static List<RaindropItem> CreateLargeTestDataSet()
    {
        var items = new List<RaindropItem>();
        for (var i = 1; i <= 100; i++)
            items.Add(new RaindropItem
            {
                Id = i,
                Link = $"https://example.com/item{i}",
                Title = $"Test Item {i} with a longer title to increase data size",
                Excerpt = $"This is a longer excerpt for item {i} to test compression efficiency with more substantial content that should compress well.",
                Cover = $"https://example.com/covers/item{i}.jpg",
                Domain = "example.com",
                Created = DateTime.UtcNow.AddDays(-i),
                Type = i % 2 == 0 ? "video" : "article"
            });
        return items;
    }

    /// <summary>
    ///     Creates test RaindropItem instances with special characters and Unicode content.
    /// </summary>
    private static List<RaindropItem> CreateTestItemsWithSpecialCharacters()
    {
        return
        [
            new RaindropItem
            {
                Id = 1,
                Link = "https://example.com/special",
                Title = "Test with émojis 🚀 and spëcial chars",
                Excerpt = "Content with 中文 and العربية text",
                Cover = "https://example.com/cover-special.jpg",
                Domain = "example.com",
                Created = DateTime.UtcNow.AddDays(-1),
                Type = "article"
            },
            new RaindropItem
            {
                Id = 2,
                Link = "https://example.com/unicode",
                Title = "Тест с кириллицей и ñoñó",
                Excerpt = "Content with symbols: @#$%^&*()_+-=[]{}|;':,.<>?",
                Cover = "https://example.com/cover-unicode.jpg",
                Domain = "example.com",
                Created = DateTime.UtcNow.AddDays(-2),
                Type = "video"
            }
        ];
    }


    /// <summary>
    ///     Manual mock implementation for ILocalStorageService since LightMock.Generator doesn't support it.
    /// </summary>
    public sealed class LocalStorageService_Mock : ILocalStorageService
    {
        private readonly Dictionary<string, bool> _containsKeyResults = new();
        private readonly Dictionary<string, object?> _getItemResults = new();
        private readonly Dictionary<string, string?> _getItemAsStringResults = new();
        private readonly Dictionary<string, Exception> _containsKeyExceptions = new();
        private readonly Dictionary<string, Exception> _getItemExceptions = new();
        private readonly Dictionary<string, Exception> _setItemExceptions = new();
        private readonly Dictionary<string, Exception> _removeItemExceptions = new();

        public void SetupContainKeyAsync(string key, bool result)
        {
            _containsKeyResults[key] = result;
        }

        public void SetupGetItemAsync<T>(string key, T? result)
        {
            _getItemResults[key] = result;
        }

        public void SetupGetItemAsStringAsync(string key, string? result)
        {
            _getItemAsStringResults[key] = result;
        }

        public void SetupContainKeyAsyncThrows(string key, Exception exception)
        {
            _containsKeyExceptions[key] = exception;
        }

        public void SetupGetItemAsyncThrows<T>(string key, Exception exception)
        {
            _getItemExceptions[key] = exception;
        }

        public void SetupSetItemAsyncThrows<T>(string key, Exception exception)
        {
            _setItemExceptions[key] = exception;
        }

        public void SetupRemoveItemAsyncThrows(string key, Exception exception)
        {
            _removeItemExceptions[key] = exception;
        }

        public ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            if (_containsKeyExceptions.TryGetValue(key, out var exception)) throw exception;

            var result = _containsKeyResults.ContainsKey(key) && _containsKeyResults[key];

            return ValueTask.FromResult(result);
        }

        public ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (_getItemExceptions.TryGetValue(key, out var exception)) throw exception;

            if (_getItemResults.TryGetValue(key, out var result))
            {
                if (result is T typedResult) return ValueTask.FromResult<T?>(typedResult);
                if (result != null) return ValueTask.FromResult<T?>(default);
            }

            return ValueTask.FromResult<T?>(default);
        }

        public ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default)
        {
            var result = _getItemAsStringResults.TryGetValue(key, out var value) ? value : null;

            return ValueTask.FromResult(result);
        }

        public ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
        {
            if (_setItemExceptions.TryGetValue(key, out var exception)) throw exception;

            _getItemResults[key] = data;
            _containsKeyResults[key] = true; // Ensure ContainKeyAsync returns true for stored items
            return ValueTask.CompletedTask;
        }

        public ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default)
        {
            _getItemAsStringResults[key] = data;
            _containsKeyResults[key] = true; // Ensure ContainKeyAsync returns true for stored items
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        {
            if (_removeItemExceptions.TryGetValue(key, out var exception)) throw exception;

            _getItemResults.Remove(key);
            _getItemAsStringResults.Remove(key);
            _containsKeyResults.Remove(key);
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            foreach (var key in keys)
            {
                _getItemResults.Remove(key);
                _getItemAsStringResults.Remove(key);
                _containsKeyResults.Remove(key);
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            _getItemResults.Clear();
            _getItemAsStringResults.Clear();
            _containsKeyResults.Clear();
            return ValueTask.CompletedTask;
        }

        public ValueTask<int> LengthAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask<int>(_getItemResults.Count);
        }

        public ValueTask<string?> KeyAsync(int index, CancellationToken cancellationToken = default)
        {
            var keys = _getItemResults.Keys.ToArray();
            return new ValueTask<string?>(index < keys.Length ? keys[index] : null);
        }

        public ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask<IEnumerable<string>>(_getItemResults.Keys.ToArray());
        }

#pragma warning disable CS0067 // Event is never used
        public event EventHandler<ChangingEventArgs>? Changing;
        public event EventHandler<ChangedEventArgs>? Changed;
#pragma warning restore CS0067
    }

    /// <summary>
    ///     Test scope for managing dependencies and mocks in cache tests.
    /// </summary>
    public sealed class TestScope : IDisposable
    {
        public TestScope()
        {
            RaindropAPI_Mock = new Mock<IRaindropAPI>();
            Logger_Mock = new Mock<ILogger<RaindropItemsCache>>();
            Logger = new Logger_Spy<RaindropItemsCache>();
            LocalStorageService_Mock = new LocalStorageService_Mock();
            JsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            Cache = new RaindropItemsCache(LocalStorageService_Mock, Logger);
        }

        public Mock<IRaindropAPI> RaindropAPI_Mock { get; }
        public Mock<ILogger<RaindropItemsCache>> Logger_Mock { get; }
        public Logger_Spy<RaindropItemsCache> Logger { get; }
        public LocalStorageService_Mock LocalStorageService_Mock { get; }
        public RaindropItemsCache Cache { get; }
        public JsonSerializerOptions JsonOptions { get; }

        public void Dispose()
        {
            // No resources to dispose in this implementation
        }
    }

    /// <summary>
    ///     Test logger implementation that captures log entries for verification in tests.
    /// </summary>
    public sealed class Logger_Spy<T> : ILogger<T>
    {
        private readonly List<LogEntry> _logEntries = [];

        public IReadOnlyList<LogEntry> LogEntries => _logEntries.AsReadOnly();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            _logEntries.Add(new LogEntry(logLevel, eventId, message, exception));
        }
    }

    /// <summary>
    ///     Represents a captured log entry for test verification.
    /// </summary>
    /// <param name="LogLevel">The log level of the entry.</param>
    /// <param name="EventId">The event ID of the entry.</param>
    /// <param name="Message">The formatted log message.</param>
    /// <param name="Exception">The exception associated with the log entry, if any.</param>
    public sealed record LogEntry(
        LogLevel LogLevel,
        EventId EventId,
        string Message,
        Exception? Exception);
}