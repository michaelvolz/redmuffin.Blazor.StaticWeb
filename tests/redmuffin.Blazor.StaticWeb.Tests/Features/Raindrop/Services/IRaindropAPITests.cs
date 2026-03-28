namespace redmuffin.Blazor.StaticWeb.Tests.Features.Raindrop.Services;

[Category("Feature:Raindrop")]
[Category("Unit")]
public partial class IRaindropAPITests
{
    [Test]
    public async Task DummyRaindropAPI_GetArticlesAsync_Should_Return_Valid_Articles_When_File_Exists()
    {
        // Arrange
        using var scope = CreateDummyAPITestScope();
        var api = scope.DummyAPI;
        ArgumentNullException.ThrowIfNull(api);

        // Act
        var result = await api.GetArticlesAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result).IsNotNull();
            await Assert.That(result.Count()).IsGreaterThan(0);

            var firstArticle = result.First();
            await Assert.That(firstArticle.Id).IsGreaterThan(0);
            await Assert.That(firstArticle.Title).IsNotEmpty();
            await Assert.That(firstArticle.Link).IsNotEmpty();
            await Assert.That(firstArticle.Type).IsEqualTo("article");
        }
    }


    [Test]
    [Category("Smoke")]
    public async Task DummyRaindropAPI_GetVideosAsync_Should_Return_Valid_Videos_When_File_Exists()
    {
        // Arrange
        using var scope = CreateDummyAPITestScope();
        var api = scope.DummyAPI;
        ArgumentNullException.ThrowIfNull(api);

        // Act
        var result = await api.GetVideosAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result).IsNotNull();
            await Assert.That(result.Count()).IsGreaterThan(0);

            var firstVideo = result.First();
            await Assert.That(firstVideo.Id).IsGreaterThan(0);
            await Assert.That(firstVideo.Title).IsNotEmpty();
            await Assert.That(firstVideo.Link).IsNotEmpty();
            await Assert.That(firstVideo.Type).IsEqualTo("video");
        }
    }

    [Test]
    public async Task DummyRaindropAPI_Should_Log_Success_When_Data_Loaded()
    {
        // Arrange
        using var scope = CreateDummyAPITestScope();
        var api = scope.DummyAPI;
        ArgumentNullException.ThrowIfNull(api);

        // Act
        await api.GetVideosAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(scope.GetDummyLogger().LogEntries.Any(entry =>
            entry.Message.Contains("Successfully loaded") &&
            entry.Message.Contains("videos"))).IsTrue();
    }


    [Test]
    public async Task RaindropAPI_GetArticlesAsync_Should_Return_Valid_Articles_When_API_Succeeds()
    {
        // Arrange
        using var scope = CreateRealAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        // Act
        var result = await api.GetArticlesAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result).IsNotNull();
            await Assert.That(result.Count()).IsGreaterThan(0);

            var firstArticle = result.First();
            await Assert.That(firstArticle.Id).IsGreaterThan(0);
            await Assert.That(firstArticle.Title).IsNotEmpty();
            await Assert.That(firstArticle.Link).IsNotEmpty();
        }
    }


    [Test]
    [Category("Smoke")]
    public async Task RaindropAPI_GetVideosAsync_Should_Return_Valid_Videos_When_API_Succeeds()
    {
        // Arrange
        using var scope = CreateRealAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        // Act
        var result = await api.GetVideosAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(result).IsNotNull();
            await Assert.That(result.Count()).IsGreaterThan(0);

            var firstVideo = result.First();
            await Assert.That(firstVideo.Id).IsGreaterThan(0);
            await Assert.That(firstVideo.Title).IsNotEmpty();
            await Assert.That(firstVideo.Link).IsNotEmpty();
        }
    }

    [Test]
    public async Task RaindropAPI_Should_Log_Success_When_API_Call_Succeeds()
    {
        // Arrange
        using var scope = CreateRealAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        // Act
        await api.GetVideosAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(scope.GetRealLogger().LogEntries.Any(entry =>
            entry.Message.Contains("Successfully loaded") &&
            entry.Message.Contains("videos"))).IsTrue();
    }
}