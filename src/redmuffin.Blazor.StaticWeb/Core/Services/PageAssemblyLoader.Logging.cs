namespace redmuffin.Blazor.StaticWeb.Core.Services;

public sealed partial class PageAssemblyLoader
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug,
        Message = "Skipping Home primary-journey prefetch because Save-Data is enabled.")]
    private static partial void LogSaveDataSkipsPrefetch(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug,
        Message = "Home prefetch failed for page key {PageKey}")]
    private static partial void LogHomePrefetchFailed(ILogger logger, Exception exception, string pageKey);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug,
        Message = "Loaded {AssemblyCount} assembly/assemblies for page key {PageKey}")]
    private static partial void LogPageAssembliesLoaded(ILogger logger, int assemblyCount, string pageKey);
}
