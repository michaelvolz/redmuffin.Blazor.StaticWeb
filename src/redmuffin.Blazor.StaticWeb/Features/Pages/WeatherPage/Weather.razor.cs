using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;

//using JetBrains.Annotations;
//using Microsoft.AspNetCore.Components;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.WeatherPage;

public partial class Weather : ComponentBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private WeatherForecast[]? _forecasts;

    [Inject] private ILogger<Weather> Logger { get; set; } = null!;
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        ArgumentNullException.ThrowIfNull(HttpClientFactory);
        LogOnInitializedAsync(Logger, null);

        using var httpClient = HttpClientFactory.CreateClient();
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