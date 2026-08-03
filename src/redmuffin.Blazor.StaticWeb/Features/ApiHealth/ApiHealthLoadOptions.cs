namespace redmuffin.Blazor.StaticWeb.Features.ApiHealth;

/// <summary>
/// Host-time Strategy flag for ApiHealth (synthetic vs real), captured at startup
/// without referencing the implementation assembly.
/// </summary>
public sealed class ApiHealthLoadOptions
{
    public ApiHealthLoadOptions(bool useSyntheticData)
    {
        UseSyntheticData = useSyntheticData;
    }

    public bool UseSyntheticData { get; }
}
