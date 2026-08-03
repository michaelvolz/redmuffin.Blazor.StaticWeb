namespace redmuffin.Blazor.StaticWeb.Pages.ApiHealth;

public sealed record HealthCheckItem(
    string Label,
    string Value,
    bool Passed)
{
    public string StatusIcon => Passed ? "✅" : "❌";
    public string RowCssClass => Passed ? "check-passed" : "check-failed";
}
