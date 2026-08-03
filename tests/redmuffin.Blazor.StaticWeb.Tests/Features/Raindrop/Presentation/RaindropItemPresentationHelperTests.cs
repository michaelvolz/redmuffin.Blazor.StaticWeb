using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Presentation;

namespace redmuffin.Blazor.StaticWeb.Tests.Features.Raindrop.Presentation;

public class RaindropItemPresentationHelperTests
{
    [Test]
    public async Task DisplayTitle_ReturnsTitleWhenPresent()
    {
        var item = new RaindropItem
        {
            Title = "My Amazing Video"
        };

        var result = RaindropItemPresentationHelper.DisplayTitle(item);

        await Assert.That(result).IsEqualTo("My Amazing Video");
    }

    [Test]
    public async Task DisplayTitle_ReturnsPlaceholderWhenTitleIsNull()
    {
        var item = new RaindropItem
        {
            Title = null
        };

        var result = RaindropItemPresentationHelper.DisplayTitle(item);

        await Assert.That(result).IsEqualTo("No Title Available");
    }

    [Test]
    public async Task DisplayTitle_ReturnsPlaceholderWhenTitleIsEmpty()
    {
        var item = new RaindropItem
        {
            Title = string.Empty
        };

        var result = RaindropItemPresentationHelper.DisplayTitle(item);

        await Assert.That(result).IsEqualTo("No Title Available");
    }

    [Test]
    public async Task DisplayExcerpt_ReturnsExcerptWhenPresent()
    {
        var item = new RaindropItem
        {
            Excerpt = "A brief description of the content"
        };

        var result = RaindropItemPresentationHelper.DisplayExcerpt(item);

        await Assert.That(result).IsEqualTo("A brief description of the content");
    }

    [Test]
    public async Task DisplayExcerpt_ReturnsPlaceholderWhenExcerptIsNull()
    {
        var item = new RaindropItem
        {
            Excerpt = null
        };

        var result = RaindropItemPresentationHelper.DisplayExcerpt(item);

        await Assert.That(result).IsEqualTo("No Excerpt Available");
    }

    [Test]
    public async Task DisplayExcerpt_ReturnsPlaceholderWhenExcerptIsEmpty()
    {
        var item = new RaindropItem
        {
            Excerpt = string.Empty
        };

        var result = RaindropItemPresentationHelper.DisplayExcerpt(item);

        await Assert.That(result).IsEqualTo("No Excerpt Available");
    }

    [Test]
    public async Task DisplayExcerpt_TruncatesAndAppendsEllipsisWhenExcerptExceeds250Chars()
    {
        var item = new RaindropItem
        {
            Excerpt = new string('a', 300)
        };

        var result = RaindropItemPresentationHelper.DisplayExcerpt(item);

        await Assert.That(result.Length).IsEqualTo(253); // 250 + "..."
        await Assert.That(result.EndsWith("...", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task DisplayExcerpt_ReturnsFullExcerptWhenExactly250Chars()
    {
        var item = new RaindropItem
        {
            Excerpt = new string('b', 250)
        };

        var result = RaindropItemPresentationHelper.DisplayExcerpt(item);

        await Assert.That(result.Length).IsEqualTo(250);
        await Assert.That(result).IsEqualTo(new string('b', 250));
    }

    [Test]
    public async Task DisplayExcerpt_ReturnsFullExcerptWhenUnder250Chars()
    {
        var item = new RaindropItem
        {
            Excerpt = new string('c', 249)
        };

        var result = RaindropItemPresentationHelper.DisplayExcerpt(item);

        await Assert.That(result.Length).IsEqualTo(249);
    }
}
