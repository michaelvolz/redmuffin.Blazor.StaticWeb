namespace redmuffin.Blazor.StaticWeb.Features.ApiHealth;

public sealed record ApiHealthData(
    string Message,
    string ResponseTimeFormatted,
    IReadOnlyList<HealthCheckItem> Checks);
