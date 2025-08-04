namespace redmuffin.Blazor.StaticWeb.Features.Pages.DebugPage;

public partial class LocalStorageDebug
{
    private static readonly Action<ILogger, Exception?> LogDiagnosticsFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, nameof(LogDiagnosticsFailed)),
            "Failed to run localStorage diagnostics");
}