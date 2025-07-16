using System.Net.Http.Json;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;

//using JetBrains.Annotations;
//using Microsoft.AspNetCore.Components;

namespace redmuffin.Blazor.StaticWeb.Features.Pages.WeatherPage;

public partial class Weather : ComponentBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [UsedImplicitly] private WeatherForecast[]? _forecasts;

    [Inject] private ILogger<Weather> Logger { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
#pragma warning disable CA1848
        Logger.LogWarning("Weather OnInitializedAsync(v1)");
#pragma warning restore CA1848

#pragma warning disable IL2026
        _forecasts = await Http.GetFromJsonAsync<WeatherForecast[]>("sample-data/weather.json", JsonOptions).ConfigureAwait(false);
#pragma warning restore IL2026
    }

    public class WeatherForecast
    {
        public DateOnly Date { get; set; }

        public int TemperatureC { get; set; }

        public string? Summary { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}