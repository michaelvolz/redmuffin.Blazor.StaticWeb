using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Pages.Weather;

#pragma warning disable MA0049 // Type name matches namespace — standard Blazor component pattern
public partial class Weather
#pragma warning restore MA0049
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Weather OnInitializedAsync(v1)")]
    private static partial void LogOnInitializedCalled(ILogger logger);
}
