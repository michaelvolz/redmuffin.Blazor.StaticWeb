namespace redmuffin.Blazor.StaticWeb.Features.ApiHealth;

public sealed record ApiHealthViewModel
{
    public static readonly ApiHealthViewModel Idle = new();

    public bool IsIdle { get; private init; } = true;
    public bool IsLoading { get; private init; }
    public bool IsHealthy { get; private init; }
    public bool IsUnhealthy { get; private init; }

    public string StatusIcon { get; private init; } = "🫀";
    public string StatusText { get; private init; } = "Ready for a health check";
    public string StatusDetail { get; private init; } = "Click the button below to verify the API is reachable and responding.";

    public string? ResponseMessage { get; private init; }
    public string? ResponseTimeFormatted { get; private init; }
    public string? ErrorDetail { get; private init; }
    public string ButtonText { get; private init; } = "Run Health Check";
    public IReadOnlyList<HealthCheckItem> Checks { get; private init; } = [];

    public static ApiHealthViewModel Loading() => new()
    {
        IsIdle = false,
        IsLoading = true,
        StatusIcon = "⏳",
        StatusText = "Checking API health...",
        StatusDetail = "Sending a request to the API endpoint and waiting for a response.",
        ButtonText = "Checking..."
    };

    public static ApiHealthViewModel Healthy(ApiHealthData data) => new()
    {
        IsIdle = false,
        IsHealthy = true,
        StatusIcon = "✅",
        StatusText = "API is healthy",
        StatusDetail = $"Response received in {data.ResponseTimeFormatted}. All checks passed.",
        ResponseMessage = data.Message,
        ResponseTimeFormatted = data.ResponseTimeFormatted,
        Checks = data.Checks
    };

    public static ApiHealthViewModel Unhealthy(string error) => new()
    {
        IsIdle = false,
        IsUnhealthy = true,
        StatusIcon = "❌",
        StatusText = "API is unreachable",
        StatusDetail = "The health check failed. See the error details below.",
        ErrorDetail = error
    };
}
