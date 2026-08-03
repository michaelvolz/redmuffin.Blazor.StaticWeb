namespace redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Models;

/// <summary>
///     Configuration settings for placeholder generation.
/// </summary>
internal sealed class PlaceholderConfiguration
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PlaceholderConfiguration" /> class with default values.
    /// </summary>
    public PlaceholderConfiguration()
    {
        Width = 400;
        Height = 200;
        BackgroundColor = "#f5f5f5";
        BorderColor = "#ddd";
        BorderWidth = 2;
        TextColor = "#999";
        FontFamily = "Arial, sans-serif";
        FontSize = 16;
        DefaultText = "No Image Available";
    }

    /// <summary>
    ///     Gets or sets the width of the placeholder in pixels.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    ///     Gets or sets the height of the placeholder in pixels.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    ///     Gets or sets the background color of the placeholder.
    /// </summary>
    public string BackgroundColor { get; set; }

    /// <summary>
    ///     Gets or sets the border color of the placeholder.
    /// </summary>
    public string BorderColor { get; set; }

    /// <summary>
    ///     Gets or sets the border width of the placeholder in pixels.
    /// </summary>
    public int BorderWidth { get; set; }

    /// <summary>
    ///     Gets or sets the text color of the placeholder.
    /// </summary>
    public string TextColor { get; set; }

    /// <summary>
    ///     Gets or sets the font family of the placeholder text.
    /// </summary>
    public string FontFamily { get; set; }

    /// <summary>
    ///     Gets or sets the font size of the placeholder text.
    /// </summary>
    public int FontSize { get; set; }

    /// <summary>
    ///     Gets or sets the default text to display when no specific reason is provided.
    /// </summary>
    public string DefaultText { get; set; }
}