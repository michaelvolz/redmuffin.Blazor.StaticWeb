namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth;

public sealed record ApiHealthData(
    string Message,
    string ResponseTimeFormatted,
    IReadOnlyList<HealthCheckItem> Checks);
