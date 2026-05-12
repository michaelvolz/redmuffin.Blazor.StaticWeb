using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using redmuffin.Blazor.StaticWeb.Common.Validation;

namespace redmuffin.Blazor.StaticWeb.Features.RaindropItems.Models;

/// <summary>
///     Pruned version of RaindropItem containing only essential fields for UI display.
///     Optimized for cache storage with minimal data footprint and efficient JSON serialization.
/// </summary>
public sealed class PrunedRaindropItem
{
    /// <summary>
    ///     Gets or sets the unique identifier for the raindrop item.
    /// </summary>
    [JsonPropertyName("i")]
    [Range(1, long.MaxValue, ErrorMessage = "ID must be a positive value.")]
    public long Id { get; set; }

    /// <summary>
    ///     Gets or sets the URL link of the raindrop item.
    /// </summary>
    [JsonPropertyName("l")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [AbsoluteUrl(ErrorMessage = "Link must be a valid absolute URI.")]
    public string? Link { get; set; }

    /// <summary>
    ///     Gets or sets the title of the raindrop item.
    /// </summary>
    [JsonPropertyName("t")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [MaxLength(500, ErrorMessage = "Title cannot exceed 500 characters.")]
    public string? Title { get; set; }

    /// <summary>
    ///     Gets or sets the excerpt or description of the raindrop item.
    /// </summary>
    [JsonPropertyName("e")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [MaxLength(2000, ErrorMessage = "Excerpt cannot exceed 2000 characters.")]
    public string? Excerpt { get; set; }

    /// <summary>
    ///     Gets or sets the cover image URL of the raindrop item.
    /// </summary>
    [JsonPropertyName("c")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [AbsoluteUrl(ErrorMessage = "Cover must be a valid absolute URI.")]
    public string? Cover { get; set; }

    /// <summary>
    ///     Validates the integrity of the pruned raindrop item data.
    /// </summary>
    /// <returns>True if the item data is valid; otherwise, false.</returns>
    public bool IsValid() =>
        Validator.TryValidateObject(this, new(this), null, validateAllProperties: true);

    /// <summary>
    ///     Validates the integrity of the pruned raindrop item data and throws an exception if invalid.
    /// </summary>
    /// <exception cref="ValidationException">Thrown when the item data is invalid.</exception>
    public void ValidateOrThrow()
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(this, new(this), results, validateAllProperties: true))
            throw new ValidationException(results[0], null, this);
    }
}
