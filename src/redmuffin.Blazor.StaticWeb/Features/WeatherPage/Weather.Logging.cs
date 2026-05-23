namespace redmuffin.Blazor.StaticWeb.Features.WeatherPage;

public partial class Weather
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Weather OnInitializedAsync(v1)")]
    private static partial void LogOnInitializedCalled(ILogger logger);
}
