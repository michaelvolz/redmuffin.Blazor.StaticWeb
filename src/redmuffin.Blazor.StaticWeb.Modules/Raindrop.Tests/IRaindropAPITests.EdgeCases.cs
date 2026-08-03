namespace redmuffin.Blazor.StaticWeb.Modules.Raindrop.Tests;

[Category("Feature:Raindrop")]
public sealed partial class IRaindropAPITests
{
    [Test]
    public async Task DummyRaindropAPI_GetArticlesAsync_Should_Handle_Cancellation_Token()
    {
        using var scope = CreateDummyAPITestScope();
        var api = scope.DummyAPI;
        ArgumentNullException.ThrowIfNull(api);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);

        await Assert.ThrowsAsync<OperationCanceledException>(() => api.GetArticlesAsync(cts.Token));
    }

    [Test]
    public async Task DummyRaindropAPI_GetArticlesAsync_Should_Return_Empty_Collection_When_File_Missing()
    {
        using var scope = CreateDummyAPITestScopeWithMissingFiles();
        var api = scope.DummyAPI;
        ArgumentNullException.ThrowIfNull(api);

        var result = await api.GetArticlesAsync(CancellationToken.None).ConfigureAwait(false);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task DummyRaindropAPI_GetVideosAsync_Should_Handle_Cancellation_Token()
    {
        using var scope = CreateDummyAPITestScope();
        var api = scope.DummyAPI;
        ArgumentNullException.ThrowIfNull(api);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);

        await Assert.ThrowsAsync<OperationCanceledException>(() => api.GetVideosAsync(cts.Token));
    }

    [Test]
    [Category("Smoke")]
    public async Task DummyRaindropAPI_GetVideosAsync_Should_Return_Empty_Collection_When_File_Missing()
    {
        using var scope = CreateDummyAPITestScopeWithMissingFiles();
        var api = scope.DummyAPI;
        ArgumentNullException.ThrowIfNull(api);

        var result = await api.GetVideosAsync(CancellationToken.None).ConfigureAwait(false);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task RaindropAPI_GetArticlesAsync_Should_Handle_Cancellation_Token()
    {
        using var scope = CreateRealAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);

        await Assert.ThrowsAsync<OperationCanceledException>(() => api.GetArticlesAsync(cts.Token));
    }

    [Test]
    public async Task RaindropAPI_GetArticlesAsync_Should_Throw_HttpRequestException_When_API_Fails()
    {
        using var scope = CreateFailingAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        await Assert.ThrowsAsync<HttpRequestException>(() => api.GetArticlesAsync(CancellationToken.None));
    }

    [Test]
    public async Task RaindropAPI_GetArticlesAsync_Should_Throw_InvalidOperationException_When_Response_Malformed()
    {
        using var scope = CreateMalformedResponseAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        await Assert.ThrowsAsync<InvalidOperationException>(() => api.GetArticlesAsync(CancellationToken.None));
    }

    [Test]
    public async Task RaindropAPI_GetVideosAsync_Should_Handle_Cancellation_Token()
    {
        using var scope = CreateRealAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);

        await Assert.ThrowsAsync<OperationCanceledException>(() => api.GetVideosAsync(cts.Token));
    }

    [Test]
    public async Task RaindropAPI_GetVideosAsync_Should_Throw_HttpRequestException_When_API_Fails()
    {
        using var scope = CreateFailingAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        await Assert.ThrowsAsync<HttpRequestException>(() => api.GetVideosAsync(CancellationToken.None));
    }

    [Test]
    public async Task RaindropAPI_GetVideosAsync_Should_Throw_InvalidOperationException_When_Response_Malformed()
    {
        using var scope = CreateMalformedResponseAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        await Assert.ThrowsAsync<InvalidOperationException>(() => api.GetVideosAsync(CancellationToken.None));
    }

    [Test]
    public async Task RaindropAPI_Should_Log_Error_When_API_Call_Fails()
    {
        using var scope = CreateFailingAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        await Assert.ThrowsAsync<HttpRequestException>(() => api.GetVideosAsync(CancellationToken.None));

        await Assert.That(scope.GetRealLogger().LogEntries.Any(entry =>
            entry.Message.Contains("HTTP request error in") &&
            entry.Message.Contains("GetVideosAsync"))).IsTrue();
    }
}
