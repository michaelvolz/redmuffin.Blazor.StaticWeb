using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Pages.Weather;

#pragma warning disable MA0049 // Type name matches namespace — standard Blazor component pattern
public partial class Weather : ComponentBase
#pragma warning restore MA0049
{
    // Lazy: the source-generated context binds only after static init; Web defaults keep camelCase.
    private static readonly Lazy<JsonSerializerOptions> LazyJsonOptions = new(() =>
        new(JsonSerializerDefaults.Web) { TypeInfoResolver = WeatherSerializerContext.Default }
    );

    private readonly ILogger<Weather> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private WeatherForecast[]? _forecasts;

    public Weather(ILogger<Weather> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory =
            httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    private static JsonSerializerOptions JsonOptions => LazyJsonOptions.Value;

    protected override async Task OnInitializedAsync()
    {
        LogOnInitializedCalled(_logger);

        using var httpClient = _httpClientFactory.CreateClient();
        // JsonTypeInfo overload: trim-safe on .NET 10 (IL2026).
        var forecastTypeInfo =
            (JsonTypeInfo<WeatherForecast[]>)JsonOptions.GetTypeInfo(typeof(WeatherForecast[]));
        _forecasts = await httpClient
            .GetFromJsonAsync("sample-data/weather.json", forecastTypeInfo)
            .ConfigureAwait(false);
    }

    public class WeatherForecast
    {
        public DateOnly Date { get; set; }

        public int TemperatureC { get; set; }

        public string? Summary { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
