using redmuffin.Blazor.StaticWeb.Common;

namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Tests;

[Category("Feature:Raindrop")]
[Category("Unit")]
public partial class IRaindropAPITests
{
    [Test]
    public async Task DummyRaindropAPI_GetArticlesAsync_Should_Return_Valid_Articles_When_File_Exists()
    {
        using var scope = CreateDummyAPITestScope();
        var api = scope.DummyAPI;
        ArgumentNullException.ThrowIfNull(api);

        var result = await api.GetArticlesAsync(CancellationToken.None).ConfigureAwait(false);

        using (Assert.Multiple())
        {
            await Assert.That(result.IsSuccess).IsTrue();
            await Assert.That(result.Value.Count).IsGreaterThan(0);

            var firstArticle = result.Value[0];
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
        using var scope = CreateDummyAPITestScope();
        var api = scope.DummyAPI;
        ArgumentNullException.ThrowIfNull(api);

        var result = await api.GetVideosAsync(CancellationToken.None).ConfigureAwait(false);

        using (Assert.Multiple())
        {
            await Assert.That(result.IsSuccess).IsTrue();
            await Assert.That(result.Value.Count).IsGreaterThan(0);

            var firstVideo = result.Value[0];
            await Assert.That(firstVideo.Id).IsGreaterThan(0);
            await Assert.That(firstVideo.Title).IsNotEmpty();
            await Assert.That(firstVideo.Link).IsNotEmpty();
            await Assert.That(firstVideo.Type).IsEqualTo("video");
        }
    }

    [Test]
    public async Task DummyRaindropAPI_Should_Log_Success_When_Data_Loaded()
    {
        using var scope = CreateDummyAPITestScope();
        var api = scope.DummyAPI;
        ArgumentNullException.ThrowIfNull(api);

        await api.GetVideosAsync(CancellationToken.None).ConfigureAwait(false);

        await Assert.That(scope.GetDummyLogger().LogEntries.Any(entry =>
            entry.Message.Contains("Successfully loaded") &&
            entry.Message.Contains("videos"))).IsTrue();
    }

    [Test]
    public async Task RaindropAPI_GetArticlesAsync_Should_Return_Valid_Articles_When_API_Succeeds()
    {
        using var scope = CreateRealAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        var result = await api.GetArticlesAsync(CancellationToken.None).ConfigureAwait(false);

        using (Assert.Multiple())
        {
            await Assert.That(result.IsSuccess).IsTrue();
            await Assert.That(result.Value.Count).IsGreaterThan(0);

            var firstArticle = result.Value[0];
            await Assert.That(firstArticle.Id).IsGreaterThan(0);
            await Assert.That(firstArticle.Title).IsNotEmpty();
            await Assert.That(firstArticle.Link).IsNotEmpty();
        }
    }

    [Test]
    [Category("Smoke")]
    public async Task RaindropAPI_GetVideosAsync_Should_Return_Valid_Videos_When_API_Succeeds()
    {
        using var scope = CreateRealAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        var result = await api.GetVideosAsync(CancellationToken.None).ConfigureAwait(false);

        using (Assert.Multiple())
        {
            await Assert.That(result.IsSuccess).IsTrue();
            await Assert.That(result.Value.Count).IsGreaterThan(0);

            var firstVideo = result.Value[0];
            await Assert.That(firstVideo.Id).IsGreaterThan(0);
            await Assert.That(firstVideo.Title).IsNotEmpty();
            await Assert.That(firstVideo.Link).IsNotEmpty();
        }
    }

    [Test]
    public async Task RaindropAPI_Should_Log_Success_When_API_Call_Succeeds()
    {
        using var scope = CreateRealAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        await api.GetVideosAsync(CancellationToken.None).ConfigureAwait(false);

        await Assert.That(scope.GetRealLogger().LogEntries.Any(entry =>
            entry.Message.Contains("Successfully loaded") &&
            entry.Message.Contains("videos"))).IsTrue();
    }
}
