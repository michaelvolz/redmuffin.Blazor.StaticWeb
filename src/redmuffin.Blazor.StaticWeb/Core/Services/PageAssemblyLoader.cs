using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.JSInterop;
using redmuffin.Blazor.StaticWeb.Common.Abstractions;

namespace redmuffin.Blazor.StaticWeb.Core.Services;

/// <summary>
///     Shared navigate + Home-prefetch loader. Catalog-driven; empty page keys no-op.
/// </summary>
public sealed partial class PageAssemblyLoader(
    LazyAssemblyLoader assemblyLoader,
    IJSRuntime jsRuntime,
    ILogger<PageAssemblyLoader> logger) : IPageAssemblyLoader
{
    private readonly LazyAssemblyLoader _assemblyLoader =
        assemblyLoader ?? throw new ArgumentNullException(nameof(assemblyLoader));

    private readonly IJSRuntime _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    private readonly ILogger<PageAssemblyLoader> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly List<Assembly> _loadedAssemblies = [];
    private readonly ConcurrentDictionary<string, Task> _loadTasks = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _loadedAssembliesGate = new();

    public IReadOnlyList<Assembly> LoadedAssemblies
    {
        get
        {
            lock (_loadedAssembliesGate)
                return _loadedAssemblies.ToArray();
        }
    }

    public Task EnsureLoadedAsync(string pageKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageKey);

        if (!PageAssemblyCatalog.TryGetAssemblies(pageKey, out var assemblyFileNames) ||
            assemblyFileNames.Count == 0)
            return Task.CompletedTask;

        return _loadTasks.GetOrAdd(
            pageKey,
            key => LoadPageAssembliesWithRetrySlotAsync(key, assemblyFileNames, cancellationToken));
    }

    private async Task LoadPageAssembliesWithRetrySlotAsync(
        string pageKey,
        IReadOnlyList<string> assemblyFileNames,
        CancellationToken cancellationToken)
    {
        try
        {
            await LoadPageAssembliesAsync(pageKey, assemblyFileNames, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            _loadTasks.TryRemove(pageKey, out _);
            throw;
        }
    }

    public async Task PrefetchHomePrimaryJourneysAsync(CancellationToken cancellationToken = default)
    {
        if (await IsSaveDataEnabledAsync(cancellationToken).ConfigureAwait(false))
        {
            LogSaveDataSkipsPrefetch(_logger);
            return;
        }

        foreach (var pageKey in PageAssemblyCatalog.HomePrefetchPageKeys)
        {
            try
            {
                await EnsureLoadedAsync(pageKey, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Speculative: real navigation still loads via OnNavigateAsync.
                LogHomePrefetchFailed(_logger, exception, pageKey);
            }
        }
    }

    private async Task LoadPageAssembliesAsync(
        string pageKey,
        IReadOnlyList<string> assemblyFileNames,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var assemblies = await _assemblyLoader
            .LoadAssembliesAsync(assemblyFileNames)
            .ConfigureAwait(false);

        lock (_loadedAssembliesGate)
        {
            foreach (var assembly in assemblies)
            {
                if (!_loadedAssemblies.Contains(assembly))
                    _loadedAssemblies.Add(assembly);
            }
        }

        LogPageAssembliesLoaded(_logger, assemblyFileNames.Count, pageKey);
    }

    private async Task<bool> IsSaveDataEnabledAsync(CancellationToken cancellationToken)
    {
        try
        {
            _ = cancellationToken;
            return await _jsRuntime
                .InvokeAsync<bool>(
                    "eval",
                    "!!(navigator.connection && navigator.connection.saveData === true)")
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is JSException or InvalidOperationException)
        {
            return false;
        }
    }
}
