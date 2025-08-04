using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.WeatherPage;

public partial class Weather
{
    private static readonly Action<ILogger, Exception?> LogOnInitializedAsync = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(1, "WeatherOnInitializedAsync"),
        "Weather OnInitializedAsync(v1)");
}
