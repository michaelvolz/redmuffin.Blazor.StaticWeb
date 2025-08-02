using System;
using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.DebugPage.CacheResetPage;

public partial class CacheReset
{
    private static readonly Action<ILogger, Exception?> LogCacheResetStarted =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogCacheResetStarted)),
            "Starting cache reset operation");

    private static readonly Action<ILogger, int, Exception?> LogCacheResetCompleted =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(2, nameof(LogCacheResetCompleted)),
            "Cache reset completed successfully. Items cleared: {ItemsCleared}");

    private static readonly Action<ILogger, Exception?> LogCacheResetError =
        LoggerMessage.Define(LogLevel.Error, new EventId(3, nameof(LogCacheResetError)),
            "Error occurred during cache reset");
}
