namespace redmuffin.Blazor.StaticWeb.Pages.ApiHealth;

public sealed record ApiHealthData(
    string Message,
    string ResponseTimeFormatted,
    IReadOnlyList<HealthCheckItem> Checks);
