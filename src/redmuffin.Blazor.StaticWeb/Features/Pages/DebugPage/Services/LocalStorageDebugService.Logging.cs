namespace redmuffin.Blazor.StaticWeb.Features.Pages.DebugPage.Services;

public partial class LocalStorageDebugService
{
    private static readonly Action<ILogger, bool, bool, string, Exception?> LogDiagnosticsCompleted =
        LoggerMessage.Define<bool, bool, string>(
            LogLevel.Information,
            new EventId(1, nameof(LogDiagnosticsCompleted)),
            "LocalStorage diagnostics completed: Available={IsAvailable}, BlazoredWorking={BlazoredWorking}, UsedStorage={UsedBytes}MB");

    private static readonly Action<ILogger, Exception?> LogDiagnosticsFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(2, nameof(LogDiagnosticsFailed)),
            "Failed to run localStorage diagnostics");

    private static readonly Action<ILogger, Exception?> LogLocalStorageTestFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(3, nameof(LogLocalStorageTestFailed)),
            "localStorage availability test failed");

    private static readonly Action<ILogger, string, string?, Exception?> LogBlazoredTestFailed =
        LoggerMessage.Define<string, string?>(
            LogLevel.Warning,
            new EventId(4, nameof(LogBlazoredTestFailed)),
            "Blazored localStorage test failed: expected '{Expected}', got '{Actual}'");

    private static readonly Action<ILogger, Exception?> LogBlazoredServiceFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(5, nameof(LogBlazoredServiceFailed)),
            "Blazored localStorage service test failed");

    private static readonly Action<ILogger, Exception?> LogStorageInfoFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(6, nameof(LogStorageInfoFailed)),
            "Failed to get storage info");

    private static readonly Action<ILogger, Exception?> LogJsonTestFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(7, nameof(LogJsonTestFailed)),
            "JSON serialization test failed");

    private static readonly Action<ILogger, Exception?> LogCacheKeysFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(8, nameof(LogCacheKeysFailed)),
            "Failed to get existing cache keys");
}