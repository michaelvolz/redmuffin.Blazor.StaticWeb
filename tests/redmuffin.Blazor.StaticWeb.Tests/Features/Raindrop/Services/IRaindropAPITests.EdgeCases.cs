namespace redmuffin.Blazor.StaticWeb.Tests.Features.Raindrop.Services;

[Category("Feature:Raindrop")]
public sealed partial class IRaindropAPITests
{
    [Test]
    public async Task DummyRaindropAPI_GetArticlesAsync_Should_Handle_Cancellation_Token()
    {
        // Arrange
        using var scope = CreateDummyAPITestScope();
        var api = scope.DummyAPI;
        ArgumentNullException.ThrowIfNull(api);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => api.GetArticlesAsync(cts.Token));
    }

    [Test]
    public async Task DummyRaindropAPI_GetArticlesAsync_Should_Return_Empty_Collection_When_File_Missing()
    {
        // Arrange
        using var scope = CreateDummyAPITestScopeWithMissingFiles();
        var api = scope.DummyAPI;
        ArgumentNullException.ThrowIfNull(api);

        // Act
        var result = await api.GetArticlesAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task DummyRaindropAPI_GetVideosAsync_Should_Handle_Cancellation_Token()
    {
        // Arrange
        using var scope = CreateDummyAPITestScope();
        var api = scope.DummyAPI;
        ArgumentNullException.ThrowIfNull(api);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => api.GetVideosAsync(cts.Token));
    }

    [Test]
    [Category("Smoke")]
    public async Task DummyRaindropAPI_GetVideosAsync_Should_Return_Empty_Collection_When_File_Missing()
    {
        // Arrange
        using var scope = CreateDummyAPITestScopeWithMissingFiles();
        var api = scope.DummyAPI;
        ArgumentNullException.ThrowIfNull(api);

        // Act
        var result = await api.GetVideosAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task RaindropAPI_GetArticlesAsync_Should_Handle_Cancellation_Token()
    {
        // Arrange
        using var scope = CreateRealAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => api.GetArticlesAsync(cts.Token));
    }

    [Test]
    public async Task RaindropAPI_GetArticlesAsync_Should_Throw_HttpRequestException_When_API_Fails()
    {
        // Arrange
        using var scope = CreateFailingAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => api.GetArticlesAsync(CancellationToken.None));
    }

    [Test]
    public async Task RaindropAPI_GetArticlesAsync_Should_Throw_InvalidOperationException_When_Response_Malformed()
    {
        // Arrange
        using var scope = CreateMalformedResponseAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => api.GetArticlesAsync(CancellationToken.None));
    }

    [Test]
    public async Task RaindropAPI_GetVideosAsync_Should_Handle_Cancellation_Token()
    {
        // Arrange
        using var scope = CreateRealAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(false);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => api.GetVideosAsync(cts.Token));
    }

    [Test]
    public async Task RaindropAPI_GetVideosAsync_Should_Throw_HttpRequestException_When_API_Fails()
    {
        // Arrange
        using var scope = CreateFailingAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => api.GetVideosAsync(CancellationToken.None));
    }

    [Test]
    public async Task RaindropAPI_GetVideosAsync_Should_Throw_InvalidOperationException_When_Response_Malformed()
    {
        // Arrange
        using var scope = CreateMalformedResponseAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => api.GetVideosAsync(CancellationToken.None));
    }

    [Test]
    public async Task RaindropAPI_Should_Log_Error_When_API_Call_Fails()
    {
        // Arrange
        using var scope = CreateFailingAPITestScope();
        var api = scope.RealAPI;
        ArgumentNullException.ThrowIfNull(api);

        // Act & Assert
        try
        {
            await api.GetVideosAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            // Expected exception
        }

        await Assert.That(scope.GetRealLogger().LogEntries.Any(entry =>
            entry.Message.Contains("HTTP request error in") &&
            entry.Message.Contains("GetVideosAsync"))).IsTrue();
    }
}