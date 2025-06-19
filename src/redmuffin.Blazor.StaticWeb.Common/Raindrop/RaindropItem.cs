using JetBrains.Annotations;

#pragma warning disable MA0048 //Disable warning for file name not matching type name

namespace redmuffin.Blazor.StaticWeb.Common.Raindrop;

[UsedImplicitly]
public class RaindropItem
{
	public long Id { get; set; }
	public string? Link { get; set; }
	public string? Title { get; set; }
	public string? Excerpt { get; set; }
	public string? Note { get; set; }
	public string? Type { get; set; }
	public UserReference? User { get; set; }
	public string? Cover { get; set; }
	public IList<MediaItem>? Media { get; set; }
	public IList<string>? Tags { get; set; }
	public bool Important { get; set; }
	public Reminder? Reminder { get; set; }
	public bool Removed { get; set; }
	public DateTime Created { get; set; }
	public CollectionReference? Collection { get; set; }
	public IList<Highlight>? Highlights { get; set; }
	public string? Domain { get; set; }
	public long CollectionId { get; set; }
	public long Sort { get; set; }
}