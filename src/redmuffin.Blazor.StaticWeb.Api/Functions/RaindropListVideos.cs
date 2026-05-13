using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using redmuffin.Blazor.StaticWeb.Api.Core;

#pragma warning disable CA1816, SA1204

namespace redmuffin.Blazor.StaticWeb.Api.Functions;

public sealed partial class RaindropListVideos(ILogger<RaindropListVideos> logger, IOptions<Settings> settings, IHttpClientFactory httpClientFactory)
{
    private const string TargetCollectionId = "56109697";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly Settings _settings = settings.Value;

    /// <summary>
    ///     Handles HTTP GET requests to fetch a list of videos from the Raindrop API.
    /// </summary>
    [Function("RaindropListVideos")]
    public Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData request)
    {
        Log_FunctionProcessed(logger);

        return RaindropListFetcher.FetchAsync(
            request, TargetCollectionId, _settings.RainDropTestToken ?? string.Empty, _httpClientFactory, logger,
            logFetch: Log_FetchVideos,
            logError: Log_ErrorFetchingVideos,
            cancellationToken: request.FunctionContext.CancellationToken);
    }
}
