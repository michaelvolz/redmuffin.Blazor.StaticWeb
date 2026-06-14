using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;

//using JetBrains.Annotations;
//using Microsoft.AspNetCore.Components;

namespace redmuffin.Blazor.StaticWeb.Features.WeatherPage;

public partial class Weather : ComponentBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<Weather> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private WeatherForecast[]? _forecasts;

    public Weather(ILogger<Weather> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    protected override async Task OnInitializedAsync()
    {
        LogOnInitializedCalled(_logger);

        using var httpClient = _httpClientFactory.CreateClient();
        _forecasts = await httpClient.GetFromJsonAsync<WeatherForecast[]>("sample-data/weather.json", JsonOptions).ConfigureAwait(false);
    }

    public class WeatherForecast
    {
        public DateOnly Date { get; set; }

        public int TemperatureC { get; set; }

        public string? Summary { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}