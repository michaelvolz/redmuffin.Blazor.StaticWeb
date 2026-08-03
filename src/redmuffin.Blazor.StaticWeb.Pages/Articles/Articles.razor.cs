using Mediator;
using redmuffin.Blazor.StaticWeb.Common.ImagePlaceholder;
using redmuffin.Blazor.StaticWeb.Common.Raindrop;
using redmuffin.Blazor.StaticWeb.Components.Raindrop;
using redmuffin.Blazor.StaticWeb.Modules.Raindrop.Contracts;

namespace redmuffin.Blazor.StaticWeb.Pages.Articles;

#pragma warning disable MA0049 // Type name matches namespace — standard Blazor component pattern
public partial class Articles
#pragma warning restore MA0049
{
    private const string LoadErrorMessage =
        "Unable to load items. Please check your internet connection and try refreshing the page.";

    private const string RefreshErrorMessage =
        "Unable to refresh. Please check your internet connection and try again.";

    private readonly RaindropPageContext _context = new();

    private readonly IImageUrlResolver _imageUrlResolver;
    private readonly IMediator _mediator;

    public Articles(
        IImageUrlResolver imageUrlResolver,
        IMediator mediator)
    {
        _imageUrlResolver = imageUrlResolver ?? throw new ArgumentNullException(nameof(imageUrlResolver));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    ///     Gets the background refresh task so callers (including tests) can
    ///     await initialization completion deterministically without polling or delays.
    /// </summary>
    public Task? BackgroundRefreshTask { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadItemsAsync().ConfigureAwait(false);
        StateHasChanged();

        BackgroundRefreshTask = Task.Run(RefreshInBackgroundAsync);
    }

    private async Task LoadItemsAsync()
    {
        try
        {
            var result = await _mediator.Send(new LoadArticlesQuery()).ConfigureAwait(false);
            await result.Match(
                async response => await ApplyItemsAsync(response.Items).ConfigureAwait(false),
                error =>
                {
                    _ = error;
                    _context.ErrorMessage = LoadErrorMessage;
                    return Task.CompletedTask;
                }).ConfigureAwait(false);
        }
        catch (Exception)
        {
            _context.ErrorMessage = LoadErrorMessage;
        }
    }

    private async Task RefreshInBackgroundAsync()
    {
        try
        {
            var result = await _mediator.Send(new RefreshArticlesCommand()).ConfigureAwait(false);
            if (result.IsFailure)
                return;

            var freshItems = result.Value.Items;
            if (_context.Items is { Count: > 0 })
            {
                if (RaindropBackgroundRefreshHelper.HasDataChanged(_context.Items, freshItems))
                    _context.BadgeState = RefreshBadgeState.Visible;
            }
            else
            {
                await ApplyItemsAsync(freshItems).ConfigureAwait(false);
            }

            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Background refresh failures are silent (same as prior orchestrator).
            _ = ex;
        }
    }

    private async Task HandleRefreshClickAsync()
    {
        if (_context.IsRefreshing)
            return;

        _context.IsRefreshing = true;
        _context.BadgeState = RefreshBadgeState.Loading;
        _context.ErrorMessage = null;
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);

        try
        {
            var result = await _mediator.Send(new RefreshArticlesCommand()).ConfigureAwait(false);
            await result.Match(
                async response =>
                {
                    await ApplyItemsAsync(response.Items).ConfigureAwait(false);
                    _context.BadgeState = RefreshBadgeState.Hidden;
                },
                error =>
                {
                    _ = error;
                    _context.ErrorMessage = RefreshErrorMessage;
                    _context.BadgeState = RefreshBadgeState.Error;
                    return Task.CompletedTask;
                }).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _context.ErrorMessage = "Request timed out. Please try again.";
            _context.BadgeState = RefreshBadgeState.Error;
        }
        catch (Exception)
        {
            _context.ErrorMessage = "An unexpected error occurred while refreshing. Please try again.";
            _context.BadgeState = RefreshBadgeState.Error;
        }
        finally
        {
            _context.IsRefreshing = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(false);
        }
    }

    private Task ApplyItemsAsync(IReadOnlyList<RaindropItem> items)
    {
        _context.ErrorMessage = null;
        _context.Items = items.ToList();
        _context.ImageUrlCache.Clear();
        return PopulateImagesIfNeededAsync();
    }

    private async Task PopulateImagesIfNeededAsync()
    {
        if (_context.Items is not { Count: > 0 })
            return;

        try
        {
            await _imageUrlResolver.PopulateImageUrlCacheAsync(
                _context.Items,
                _context.ImageUrlCache,
                () => InvokeAsync(StateHasChanged),
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Image population is best-effort; list still renders with placeholders.
            _ = ex;
        }
    }
}
