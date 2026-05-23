namespace redmuffin.Blazor.StaticWeb.Features.Pages.WeatherPage;

public partial class Weather
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Weather OnInitializedAsync(v1)")]
    private static partial void LogOnInitializedCalled(ILogger logger);
}
