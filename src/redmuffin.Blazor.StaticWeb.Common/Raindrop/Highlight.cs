namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

public class Highlight
{
	public string? Text { get; set; }
	public string? Note { get; set; }
	public DateTime Created { get; set; }
	public DateTime LastUpdate { get; set; }
	public CreatorReference? CreatorRef { get; set; }
}