namespace redmuffin.Blazor.StaticWeb.Features.Raindrop;

/// <summary>
/// Host-time Strategy flag for Raindrop (synthetic vs real), captured at startup
/// without referencing the implementation assembly.
/// </summary>
public sealed class RaindropLoadOptions
{
    public RaindropLoadOptions(bool useSyntheticData)
    {
        UseSyntheticData = useSyntheticData;
    }

    public bool UseSyntheticData { get; }
}
