using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Models;

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
    public bool IsValid() => GetValidationErrorOrNull() is null;

    /// <summary>
    ///     Validates the integrity of the pruned raindrop item data and throws an exception if invalid.
    /// </summary>
    /// <exception cref="ValidationException">Thrown when the item data is invalid.</exception>
    public void ValidateOrThrow()
    {
        var error = GetValidationErrorOrNull();
        if (error is not null)
            throw new ValidationException(error);
    }

    // Explicit validation: the DataAnnotations Validator path is reflection-based and
    // reports IL2026 on .NET 10 trimmed publishes.
    private string? GetValidationErrorOrNull()
    {
        if (Id < 1)
            return "ID must be a positive value.";

        if (Title is { Length: > 500 })
            return "Title cannot exceed 500 characters.";

        if (Excerpt is { Length: > 2000 })
            return "Excerpt cannot exceed 2000 characters.";

        if (Link is not null && !Uri.TryCreate(Link, UriKind.Absolute, out _))
            return "Link must be a valid absolute URI.";

        if (Cover is not null && !Uri.TryCreate(Cover, UriKind.Absolute, out _))
            return "Cover must be a valid absolute URI.";

        return null;
    }
}
