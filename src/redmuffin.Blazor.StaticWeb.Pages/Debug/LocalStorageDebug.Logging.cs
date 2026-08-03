using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Pages.Debug;

public partial class LocalStorageDebug
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Failed to run localStorage diagnostics")]
    private static partial void LogDiagnosticsFailed(ILogger logger, Exception exception);
}
