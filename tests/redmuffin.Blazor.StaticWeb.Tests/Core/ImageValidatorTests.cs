using ImageValidator = redmuffin.Blazor.StaticWeb.Core.ImagePlaceholder.Services.ImageValidator;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Pages.ArticlesPage.Core;

public class ImageValidatorTests
{
    [Test]
    [MethodDataSource(nameof(MapFailureReasonTestData))]
    public async Task MapFailureReason_ShouldReturnExpectedLabel(string reason, string expectedLabel)
    {
        var result = ImageValidator.MapFailureReason(reason);

        await Assert.That(result).IsEqualTo(expectedLabel);
    }

    public static IEnumerable<(string, string)> MapFailureReasonTestData()
    {
        yield return ("CORS blocked by policy", "CORS blocked");
        yield return ("HTTP 404 Not Found", "Image not found");
        yield return ("Request timeout after 30s", "Network error");
        yield return ("Invalid content type: text/html", "Invalid format");
        yield return ("Some unknown error", "Image not available");
        yield return ("", "Image not available");
        yield return ("plain text without keywords", "Image not available");
    }
}
