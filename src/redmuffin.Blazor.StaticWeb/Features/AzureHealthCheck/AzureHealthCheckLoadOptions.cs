namespace redmuffin.Blazor.StaticWeb.Features.AzureHealthCheck;

/// <summary>
/// Host-time Strategy flag for ApiHealth (synthetic vs real), captured at startup
/// without referencing the implementation assembly.
/// </summary>
public sealed class AzureHealthCheckLoadOptions
{
    public AzureHealthCheckLoadOptions(bool useSyntheticData)
    {
        UseSyntheticData = useSyntheticData;
    }

    public bool UseSyntheticData { get; }
}
