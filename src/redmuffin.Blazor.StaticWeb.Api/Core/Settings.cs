namespace redmuffin.Blazor.StaticWeb.Api.Core;

public sealed class Settings
{
    public string? RainDropClientId { get; set; } = Environment.GetEnvironmentVariable("RainDropClientID");
    public string? RainDropClientSecret { get; set; } = Environment.GetEnvironmentVariable("RainDropClientSecret");
    public string? RainDropTestToken { get; set; } = Environment.GetEnvironmentVariable("RainDropTestToken");
}