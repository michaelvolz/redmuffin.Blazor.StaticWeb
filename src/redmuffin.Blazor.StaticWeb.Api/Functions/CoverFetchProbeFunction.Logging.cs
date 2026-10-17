using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Api.Functions;

public sealed partial class CoverFetchProbeFunction
{
    [LoggerMessage(EventId = 900, Level = LogLevel.Information, Message = "CoverFetchProbe triggered, url: {Url}")]
    private static partial void Log_TriggerProcessed(ILogger logger, string url);

    [LoggerMessage(EventId = 901, Level = LogLevel.Warning, Message = "CoverFetchProbe rejected invalid url: {Url}")]
    private static partial void Log_InvalidUrl(ILogger logger, string url);

    [LoggerMessage(EventId = 902, Level = LogLevel.Warning, Message = "CoverFetchProbe rejected host: {Host}")]
    private static partial void Log_HostNotAllowed(ILogger logger, string host);

    [LoggerMessage(EventId = 903, Level = LogLevel.Error, Message = "CoverFetchProbe fetch failed for host {Host}: {Error}")]
    private static partial void Log_FetchFailed(ILogger logger, string host, string error);

    [LoggerMessage(EventId = 904, Level = LogLevel.Warning, Message = "CoverFetchProbe capped response above 5 MB: {ContentLength}")]
    private static partial void Log_SizeCapped(ILogger logger, long contentLength);

    [LoggerMessage(EventId = 905, Level = LogLevel.Information, Message = "CoverFetchProbe completed with status {StatusCode}, content type {ContentType}, {ByteCount} bytes")]
    private static partial void Log_ProbeCompleted(ILogger logger, int statusCode, string contentType, int byteCount);
}
