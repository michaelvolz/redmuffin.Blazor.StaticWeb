using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Api.Functions;

/// <summary>
///     Temporary diagnostic probe: reports what the Functions host sees when it
///     fetches an X cover URL. Allowed hosts are locked down so the probe cannot be
///     repurposed as a general fetcher. Remove after the fetch-IP question is answered.
/// </summary>
public sealed partial class CoverFetchProbeFunction(
    ILogger<CoverFetchProbeFunction> logger,
    IHttpClientFactory httpClientFactory
)
{
    private const string DefaultProbeUrl =
        "https://jf.x.com/images/media-preview/2103396779496464638";
    private const long ResponseSizeCapBytes = 5_000_000;
    private const int StreamBufferSizeBytes = 81920;

    private static readonly string[] AllowedHosts = ["jf.x.com"];

    [Function("CoverFetchProbe")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData request
    )
    {
        var (target, error) = await ResolveTargetAsync(request).ConfigureAwait(false);
        if (error is not null)
        {
            return error;
        }

        var (fetchError, result) = await FetchTargetAsync(request, target).ConfigureAwait(false);
        if (fetchError is not null)
        {
            return fetchError;
        }

        return await BuildResultResponseAsync(request, result!).ConfigureAwait(false);
    }

    private async Task<(Uri Target, HttpResponseData? Error)> ResolveTargetAsync(
        HttpRequestData request
    )
    {
        var query = QueryHelpers.ParseQuery(request.Url.Query);
        var requestedUrl = query.TryGetValue("url", out var value)
            ? value.ToString()
            : string.Empty;
        var url = requestedUrl.Length > 0 ? requestedUrl : DefaultProbeUrl;

        Log_TriggerProcessed(logger, url);

        if (
            !Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
        )
        {
            Log_InvalidUrl(logger, url);
            return (
                uri!,
                await BuildPayloadResponseAsync(
                        request,
                        HttpStatusCode.BadRequest,
                        new { error = "url must be an absolute https URL" }
                    )
                    .ConfigureAwait(false)
            );
        }

        if (!AllowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
        {
            Log_HostNotAllowed(logger, uri.Host);
            return (
                uri,
                await BuildPayloadResponseAsync(
                        request,
                        HttpStatusCode.BadRequest,
                        new { error = $"host {uri.Host} is not allowed" }
                    )
                    .ConfigureAwait(false)
            );
        }

        return (uri, null);
    }

    private async Task<(HttpResponseData? ErrorResponse, ProbeResult? Result)> FetchTargetAsync(
        HttpRequestData request,
        Uri target
    )
    {
        using var client = httpClientFactory.CreateClient();
        // The body read happens inside this scope, so the 20 s limit covers it too.
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(
            request.FunctionContext.CancellationToken
        );
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(20));
        var cancellationToken = timeoutSource.Token;

        try
        {
            using var response = await client
                .GetAsync(target, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            var result = await ReadProbeResultAsync(response, cancellationToken)
                .ConfigureAwait(false);
            return (null, result);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Log_FetchFailed(logger, target.Host, ex.Message);
            return (
                await BuildPayloadResponseAsync(
                        request,
                        HttpStatusCode.OK,
                        new { reached = false, error = ex.Message }
                    )
                    .ConfigureAwait(false),
                null
            );
        }
    }

    private async Task<ProbeResult> ReadProbeResultAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "unknown";
        var contentLength = response.Content.Headers.ContentLength;
        if (contentLength is > ResponseSizeCapBytes)
        {
            Log_SizeCapped(logger, contentLength.Value);
            return new ProbeResult(
                (int)response.StatusCode,
                contentType,
                contentLength,
                BodyRead: false,
                ByteCount: null,
                Reason: "content-length above 5 MB cap"
            );
        }

        var stream = await response
            .Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var buffer = new byte[StreamBufferSizeBytes];
            long byteCount = 0;
            int read;
            while (
                (read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0
            )
            {
                byteCount += read;
                if (byteCount > ResponseSizeCapBytes)
                {
                    return new ProbeResult(
                        (int)response.StatusCode,
                        contentType,
                        contentLength,
                        BodyRead: false,
                        ByteCount: null,
                        Reason: "streamed body above 5 MB cap"
                    );
                }
            }

            return new ProbeResult(
                (int)response.StatusCode,
                contentType,
                contentLength ?? byteCount,
                BodyRead: true,
                ByteCount: byteCount,
                Reason: null
            );
        }
        finally
        {
            await stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static Task<HttpResponseData> BuildResultResponseAsync(
        HttpRequestData request,
        ProbeResult result
    )
    {
        return BuildPayloadResponseAsync(
            request,
            HttpStatusCode.OK,
            new
            {
                reached = true,
                statusCode = result.StatusCode,
                contentType = result.ContentType,
                contentLength = result.ContentLength,
                bodyRead = result.BodyRead,
                byteCount = result.ByteCount,
                reason = result.Reason,
            }
        );
    }

    private static async Task<HttpResponseData> BuildPayloadResponseAsync(
        HttpRequestData request,
        HttpStatusCode statusCode,
        object payload
    )
    {
        var response = request.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(payload).ConfigureAwait(false);
        return response;
    }

    private sealed record ProbeResult(
        int StatusCode,
        string ContentType,
        long? ContentLength,
        bool BodyRead,
        long? ByteCount,
        string? Reason
    );
}
