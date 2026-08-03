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

        using (Assert.Multiple())
        {
            await Assert.That(result.IsSuccess).IsTrue();
            await Assert.That(result.Value.Count).IsEqualTo(0);
        }
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

        using (Assert.Multiple())
        {
            await Assert.That(result.IsSuccess).IsTrue();
            await Assert.That(result.Value.Count).IsEqualTo(0);
        }
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
    public async Task RaindropAPI_GetArticlesAsync_Should_Return_Failure_When_API_Fails()
    {
        using var scope = CreateFailingAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        var result = await api.GetArticlesAsync(CancellationToken.None).ConfigureAwait(false);

        using (Assert.Multiple())
        {
            await Assert.That(result.IsFailure).IsTrue();
            await Assert.That(result.Error).IsNotEmpty();
        }
    }

    [Test]
    public async Task RaindropAPI_GetArticlesAsync_Should_Return_Failure_When_Response_Malformed()
    {
        using var scope = CreateMalformedResponseAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        var result = await api.GetArticlesAsync(CancellationToken.None).ConfigureAwait(false);

        using (Assert.Multiple())
        {
            await Assert.That(result.IsFailure).IsTrue();
            await Assert.That(result.Error).IsNotEmpty();
        }
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
    public async Task RaindropAPI_GetVideosAsync_Should_Return_Failure_When_API_Fails()
    {
        using var scope = CreateFailingAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        var result = await api.GetVideosAsync(CancellationToken.None).ConfigureAwait(false);

        using (Assert.Multiple())
        {
            await Assert.That(result.IsFailure).IsTrue();
            await Assert.That(result.Error).IsNotEmpty();
        }
    }

    [Test]
    public async Task RaindropAPI_GetVideosAsync_Should_Return_Failure_When_Response_Malformed()
    {
        using var scope = CreateMalformedResponseAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        var result = await api.GetVideosAsync(CancellationToken.None).ConfigureAwait(false);

        using (Assert.Multiple())
        {
            await Assert.That(result.IsFailure).IsTrue();
            await Assert.That(result.Error).IsNotEmpty();
        }
    }

    [Test]
    public async Task RaindropAPI_Should_Log_Error_When_API_Call_Fails()
    {
        using var scope = CreateFailingAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        var result = await api.GetVideosAsync(CancellationToken.None).ConfigureAwait(false);

        using (Assert.Multiple())
        {
            await Assert.That(result.IsFailure).IsTrue();
            await Assert.That(scope.GetRealLogger().LogEntries.Any(entry =>
                entry.Message.Contains("HTTP request error in") &&
                entry.Message.Contains("GetVideosAsync"))).IsTrue();
        }
    }
}
