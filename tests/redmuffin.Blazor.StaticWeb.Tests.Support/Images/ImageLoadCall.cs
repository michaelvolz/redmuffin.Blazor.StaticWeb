namespace redmuffin.Blazor.StaticWeb.Tests.Support.Images;

/// <summary>
///     Recorded call to <see cref="ImagePlaceholderService_Mock.HandleImageLoadAsync" />.
/// </summary>
/// <param name="ElementId">Element identifier passed by the component.</param>
/// <param name="ItemLink">Item link passed by the component.</param>
/// <param name="LoadSuccess">True when the image load reported success.</param>
/// <param name="ImageUrlCache">Cache dictionary passed by the component.</param>
public sealed record ImageLoadCall(
    string ElementId,
    string ItemLink,
    bool LoadSuccess,
    IDictionary<string, string> ImageUrlCache
);
