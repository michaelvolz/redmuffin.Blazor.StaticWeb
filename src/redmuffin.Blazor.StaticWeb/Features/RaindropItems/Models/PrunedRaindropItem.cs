using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Features.RaindropItems.Models;

/// <summary>
///     Pruned version of RaindropItem containing only essential fields for UI display.
///     Optimized for cache storage with minimal data footprint and efficient JSON serialization.
/// </summary>
public sealed class PrunedRaindropItem
{
    private static readonly Func<PrunedRaindropItem, bool>[] Validators =
    [
        item => item.Id > 0,
        item => item.Link == null || Uri.TryCreate(item.Link, UriKind.Absolute, out _),
        item => item.Cover == null || Uri.TryCreate(item.Cover, UriKind.Absolute, out _),
        item => item.Title == null || item.Title.Length <= 500,
        item => item.Excerpt == null || item.Excerpt.Length <= 2000,
    ];

    /// <summary>
    ///     Gets or sets the unique identifier for the raindrop item.
    /// </summary>
    [JsonPropertyName("i")]
    public long Id { get; set; }

    /// <summary>
    ///     Gets or sets the URL link of the raindrop item.
    /// </summary>
    [JsonPropertyName("l")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Link { get; set; }

    /// <summary>
    ///     Gets or sets the title of the raindrop item.
    /// </summary>
    [JsonPropertyName("t")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Title { get; set; }

    /// <summary>
    ///     Gets or sets the excerpt or description of the raindrop item.
    /// </summary>
    [JsonPropertyName("e")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Excerpt { get; set; }

    /// <summary>
    ///     Gets or sets the cover image URL of the raindrop item.
    /// </summary>
    [JsonPropertyName("c")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Cover { get; set; }

    /// <summary>
    ///     Validates the integrity of the pruned raindrop item data.
    /// </summary>
    /// <returns>True if the item data is valid; otherwise, false.</returns>
    public bool IsValid() => Array.TrueForAll(Validators, v => v(this));

    /// <summary>
    ///     Validates the integrity of the pruned raindrop item data and throws an exception if invalid.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the item data is invalid.</exception>
    public void ValidateOrThrow()
    {
        if (Id <= 0)
            throw new InvalidOperationException("ID must be a positive value.");

        if (Link is not null && !Uri.TryCreate(Link, UriKind.Absolute, out _))
            throw new InvalidOperationException("Link must be a valid absolute URI.");

        if (Cover is not null && !Uri.TryCreate(Cover, UriKind.Absolute, out _))
            throw new InvalidOperationException("Cover must be a valid absolute URI.");

        if (Title?.Length > 500)
            throw new InvalidOperationException("Title cannot exceed 500 characters.");

        if (Excerpt?.Length > 2000)
            throw new InvalidOperationException("Excerpt cannot exceed 2000 characters.");
    }
}