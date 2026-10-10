using Mediator;
using redmuffin.Blazor.StaticWeb.Common;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb.Tests.Support.Mediator;

/// <summary>
///     Mediator fake that answers Raindrop page load and refresh requests with configured results.
/// </summary>
public sealed class RaindropMediator_Mock : IMediator
{
    private Result<RaindropItemsResponse> _loadResult = Result.Success(
        new RaindropItemsResponse([], IsFromCache: false, HasUpdateAvailable: false)
    );

    private Result<RaindropItemsResponse> _refreshResult = Result.Success(
        new RaindropItemsResponse([], IsFromCache: false, HasUpdateAvailable: false)
    );

    private string? _refreshFailure;

    /// <summary>
    ///     Gets the number of answered load queries.
    /// </summary>
    public int LoadCallCount { get; private set; }

    /// <summary>
    ///     Gets the number of answered refresh commands.
    /// </summary>
    public int RefreshCallCount { get; private set; }

    /// <summary>
    ///     Configures the response returned for load queries.
    /// </summary>
    /// <param name="items">Items to return.</param>
    /// <param name="isFromCache">True when the response reports cached items.</param>
    public void SetupLoad(IReadOnlyList<RaindropItem> items, bool isFromCache = false)
    {
        _loadResult = Result.Success(
            new RaindropItemsResponse(items.ToList(), isFromCache, HasUpdateAvailable: false)
        );
    }

    /// <summary>
    ///     Configures load queries to fail with the supplied error.
    /// </summary>
    /// <param name="error">Failure message.</param>
    public void SetupLoadFailure(string error = "Simulated API failure")
    {
        _loadResult = Result.Failure<RaindropItemsResponse>(error);
    }

    /// <summary>
    ///     Configures the response returned for refresh commands.
    /// </summary>
    /// <param name="items">Items to return.</param>
    public void SetupRefresh(IReadOnlyList<RaindropItem> items)
    {
        _refreshFailure = null;
        _refreshResult = Result.Success(
            new RaindropItemsResponse(items.ToList(), IsFromCache: false, HasUpdateAvailable: false)
        );
    }

    /// <summary>
    ///     Configures refresh commands to fail with the supplied error.
    /// </summary>
    /// <param name="error">Failure message.</param>
    public void SetupRefreshFailure(string error = "Simulated API failure")
    {
        _refreshFailure = error;
    }

    /// <inheritdoc />
    public ValueTask<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default
    )
    {
        if (request is LoadArticlesQuery or LoadVideosQuery)
        {
            LoadCallCount++;
            return ValueTask.FromResult((TResponse)(object)_loadResult);
        }

        if (request is RefreshArticlesCommand or RefreshVideosCommand)
        {
            RefreshCallCount++;

            // Double-refresh is gated by the page (_context.IsRefreshing), not wall-clock delay.
            if (_refreshFailure is not null)
                return ValueTask.FromResult(
                    (TResponse)(object)Result.Failure<RaindropItemsResponse>(_refreshFailure)
                );

            return ValueTask.FromResult((TResponse)(object)_refreshResult);
        }

        throw new InvalidOperationException($"Unexpected request type: {request.GetType().Name}");
    }

    /// <inheritdoc />
    public ValueTask<TResponse> Send<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default
    ) => throw new NotSupportedException();

    /// <inheritdoc />
    public ValueTask<TResponse> Send<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default
    ) => throw new NotSupportedException();

    /// <inheritdoc />
    public ValueTask<object?> Send(object message, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default
    ) => throw new NotSupportedException();

    /// <inheritdoc />
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamCommand<TResponse> command,
        CancellationToken cancellationToken = default
    ) => throw new NotSupportedException();

    /// <inheritdoc />
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamQuery<TResponse> query,
        CancellationToken cancellationToken = default
    ) => throw new NotSupportedException();

    /// <inheritdoc />
    public IAsyncEnumerable<object?> CreateStream(
        object message,
        CancellationToken cancellationToken = default
    ) => throw new NotSupportedException();

    /// <inheritdoc />
    public ValueTask Publish<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default
    )
        where TNotification : INotification => throw new NotSupportedException();

    /// <inheritdoc />
    public ValueTask Publish(object notification, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
