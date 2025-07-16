using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using AngleSharp;
using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Common.Enums;
using redmuffin.Blazor.StaticWeb.Common.Models;

namespace redmuffin.Blazor.StaticWeb.Api.Functions;

/// <summary>
///     Azure Function for batch Open Graph image processing with HTML parsing using AngleSharp.
/// </summary>
public partial class GetOpenGraphImages(ILogger<GetOpenGraphImages> logger, IHttpClientFactory httpClientFactory)
{
    // Static collections for rate limiting and circuit breaker
    private static readonly ConcurrentDictionary<string, RateLimitTracker> RateLimitTrackers = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, CircuitBreakerState> CircuitBreakers = new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly ILogger<GetOpenGraphImages> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [LoggerMessage(1, LogLevel.Information, "Processing batch request {RequestId} with {ArticleCount} articles", EventName = "BatchProcessing_Started")]
    public static partial void LogBatchProcessingStarted(ILogger logger, string requestId, int articleCount);

    [LoggerMessage(2, LogLevel.Information, "Batch request {RequestId} completed in {ElapsedMs}ms - Success: {SuccessCount}, Failed: {FailureCount}",
        EventName = "BatchProcessing_Completed")]
    public static partial void LogBatchProcessingCompleted(ILogger logger, string requestId, int elapsedMs, int successCount, int failureCount);

    [LoggerMessage(3, LogLevel.Warning, "Failed to process article {ArticleUrl}: {ErrorMessage}", EventName = "Article_ProcessingFailed")]
    public static partial void LogArticleProcessingFailed(ILogger logger, string articleUrl, string errorMessage);

    [LoggerMessage(4, LogLevel.Debug, "Extracted {ImageCount} images from {ArticleUrl} in {ElapsedMs}ms", EventName = "Article_ProcessingSuccess")]
    public static partial void LogArticleProcessingSuccess(ILogger logger, int imageCount, string articleUrl, int elapsedMs);

    [LoggerMessage(5, LogLevel.Error, "HTTP request failed for {ArticleUrl}: {StatusCode} - {ErrorMessage}", EventName = "Http_RequestFailed")]
    public static partial void LogHttpRequestFailed(ILogger logger, string articleUrl, HttpStatusCode statusCode, string errorMessage);

    [LoggerMessage(6, LogLevel.Information, "Starting parallel processing of {ArticleCount} articles with max concurrency {MaxConcurrency}",
        EventName = "ParallelProcessing_Started")]
    public static partial void LogParallelProcessingStarted(ILogger logger, int articleCount, int maxConcurrency);

    [LoggerMessage(7, LogLevel.Information, "Parallel processing completed - {ConcurrentTasks} concurrent tasks, peak memory: {PeakMemoryMB}MB",
        EventName = "ParallelProcessing_Completed")]
    public static partial void LogParallelProcessingCompleted(ILogger logger, int concurrentTasks, long peakMemoryMB);

    [LoggerMessage(8, LogLevel.Warning, "Rate limit exceeded for domain {Domain}. Requests: {RequestCount}, Time window: {TimeWindowSeconds}s",
        EventName = "RateLimit_Exceeded")]
    public static partial void LogRateLimitExceeded(ILogger logger, string domain, int requestCount, int timeWindowSeconds);

    [LoggerMessage(9, LogLevel.Information, "Retrying request for {ArticleUrl}, attempt {AttemptNumber} of {MaxAttempts}", EventName = "Request_Retry")]
    public static partial void LogRequestRetry(ILogger logger, string articleUrl, int attemptNumber, int maxAttempts);

    [LoggerMessage(10, LogLevel.Error, "Circuit breaker opened for domain {Domain} after {FailureCount} failures", EventName = "CircuitBreaker_Opened")]
    public static partial void LogCircuitBreakerOpened(ILogger logger, string domain, int failureCount);

    [LoggerMessage(11, LogLevel.Information, "Circuit breaker closed for domain {Domain}", EventName = "CircuitBreaker_Closed")]
    public static partial void LogCircuitBreakerClosed(ILogger logger, string domain);

    [LoggerMessage(12, LogLevel.Warning, "Request validation failed: {ValidationErrors}", EventName = "Request_ValidationFailed")]
    public static partial void LogRequestValidationFailed(ILogger logger, string validationErrors);

    [LoggerMessage(13, LogLevel.Warning, "Request processing timed out after {TimeoutMs}ms for batch {RequestId}", EventName = "Request_TimedOut")]
    public static partial void LogRequestTimedOut(ILogger logger, int timeoutMs, string requestId);

    [LoggerMessage(14, LogLevel.Information, "Request size validation - Articles: {ArticleCount}, Request size: {RequestSizeKB}KB",
        EventName = "Request_SizeValidation")]
    public static partial void LogRequestSizeValidation(ILogger logger, int articleCount, int requestSizeKB);

    [LoggerMessage(15, LogLevel.Error, "Failed to deserialize batch request: {ErrorMessage}", EventName = "Batch_DeserializationFailed")]
    public static partial void LogBatchDeserializationFailed(ILogger logger, Exception exception, string errorMessage);

    [LoggerMessage(16, LogLevel.Error, "Unexpected error processing batch request: {ErrorMessage}", EventName = "Batch_UnexpectedError")]
    public static partial void LogBatchUnexpectedError(ILogger logger, Exception exception, string errorMessage);

    [LoggerMessage(17, LogLevel.Error, "Failed to read request body: {ErrorMessage}", EventName = "Request_ReadBodyFailed")]
    public static partial void LogRequestReadBodyFailed(ILogger logger, Exception exception, string errorMessage);

    [Function("GetOpenGraphImages")]
    public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Add request timeout at the function level
            using var functionCts = new CancellationTokenSource(TimeSpan.FromMinutes(10)); // Maximum function timeout

            // Read and deserialize the request body with size validation
            var requestBody = await ReadRequestBodyWithValidationAsync(request, functionCts.Token).ConfigureAwait(false);
            if (requestBody == null) return new BadRequestObjectResult("Request body is empty or too large");

            var batchRequest = JsonSerializer.Deserialize<BatchImageRequest>(requestBody, JsonOptions);

            if (batchRequest == null) return new BadRequestObjectResult("Invalid request format");

            // Comprehensive request validation
            var validationResult = ValidateBatchRequest(batchRequest);
            if (!validationResult.IsValid)
            {
                LogRequestValidationFailed(_logger, string.Join(", ", validationResult.Errors));
                return new BadRequestObjectResult(new
                {
                    Error = "Request validation failed",
                    ValidationErrors = validationResult.Errors
                });
            }

            LogBatchProcessingStarted(_logger, batchRequest.RequestId, batchRequest.Articles.Count);

            // Process the batch request
            var batchResponse = await ProcessBatchRequestAsync(batchRequest).ConfigureAwait(false);

            stopwatch.Stop();
            LogBatchProcessingCompleted(_logger, batchRequest.RequestId, (int)stopwatch.ElapsedMilliseconds,
                batchResponse.SuccessCount, batchResponse.FailureCount);

            batchResponse.TotalProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;

            return new OkObjectResult(batchResponse);
        }
        catch (JsonException ex)
        {
            LogBatchDeserializationFailed(_logger, ex, ex.Message);
            return new BadRequestObjectResult("Invalid JSON format");
        }
        catch (Exception ex)
        {
            LogBatchUnexpectedError(_logger, ex, ex.Message);
            return new StatusCodeResult(500);
        }
    }

    private async Task<BatchImageResponse> ProcessBatchRequestAsync(BatchImageRequest batchRequest)
    {
        var response = InitializeBatchResponse(batchRequest);
        using var semaphore = CreateSemaphore(batchRequest);
        LogParallelProcessingStart(batchRequest);

        var tasks = CreateProcessingTasks(batchRequest, semaphore);
        using var cts = new CancellationTokenSource(batchRequest.BatchTimeoutMs);

        try
        {
            var results = await Task.WhenAll(tasks).WaitAsync(cts.Token).ConfigureAwait(false);
            PopulateResponseWithResults(response, results, batchRequest);
        }
        catch (TimeoutException)
        {
            HandleTimeoutException(response, batchRequest);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            HandleCanceledException(response, batchRequest);
        }
        catch (Exception ex)
        {
            HandleGeneralException(response, ex);
        }

        FinalizeResponse(response);
        return response;
    }

    // Sub-methods for clarity
    private static BatchImageResponse InitializeBatchResponse(BatchImageRequest batchRequest)
    {
        return new BatchImageResponse
        {
            RequestId = batchRequest.RequestId,
            StartedAt = DateTime.UtcNow,
            TotalProcessed = batchRequest.Articles.Count
        };
    }

    private static SemaphoreSlim CreateSemaphore(BatchImageRequest batchRequest)
    {
        return new SemaphoreSlim(batchRequest.MaxConcurrency, batchRequest.MaxConcurrency);
    }

    private void LogParallelProcessingStart(BatchImageRequest batchRequest)
    {
        LogParallelProcessingStarted(_logger, batchRequest.Articles.Count, batchRequest.MaxConcurrency);
    }

    private IEnumerable<Task<ArticleImageResponse>> CreateProcessingTasks(BatchImageRequest batchRequest, SemaphoreSlim semaphore)
    {
        return batchRequest.Articles.Select(article => ProcessArticleWithSemaphoreAsync(article, semaphore, batchRequest));
    }

    private void PopulateResponseWithResults(BatchImageResponse response, ArticleImageResponse[] results, BatchImageRequest batchRequest)
    {
        response.Results = results.ToList();
        response.SuccessCount = results.Count(r => r.IsSuccess);
        response.FailureCount = results.Count(r => !r.IsSuccess);
        response.CacheHitCount = results.Count(r => r.FromCache);
        response.IsSuccess = response.FailureCount == 0 || !batchRequest.StopOnFirstError;
        LogParallelProcessingCompleted(_logger, batchRequest.MaxConcurrency, CapturePeakMemory());
    }

    private void HandleTimeoutException(BatchImageResponse response, BatchImageRequest batchRequest)
    {
        response.IsSuccess = false;
        response.ErrorMessages.Add($"Batch processing timed out after {batchRequest.BatchTimeoutMs.ToString(CultureInfo.InvariantCulture)}ms");
        LogRequestTimedOut(_logger, batchRequest.BatchTimeoutMs, batchRequest.RequestId);
    }

    private void HandleCanceledException(BatchImageResponse response, BatchImageRequest batchRequest)
    {
        response.IsSuccess = false;
        response.ErrorMessages.Add($"Batch processing was cancelled due to timeout after {batchRequest.BatchTimeoutMs.ToString(CultureInfo.InvariantCulture)}ms");
        LogRequestTimedOut(_logger, batchRequest.BatchTimeoutMs, batchRequest.RequestId);
    }

    private static void HandleGeneralException(BatchImageResponse response, Exception ex)
    {
        response.IsSuccess = false;
        response.ErrorMessages.Add($"Batch processing failed: {ex.Message}");
    }

    private static void FinalizeResponse(BatchImageResponse response)
    {
        response.ProcessedAt = DateTime.UtcNow;
    }

    private static long CapturePeakMemory()
    {
        var initialMemory = GC.GetTotalMemory(false);
        var endMemory = GC.GetTotalMemory(false);
        return (endMemory - initialMemory) / (1024 * 1024);
    }

    private async Task<ArticleImageResponse> ProcessArticleWithSemaphoreAsync(ArticleImageRequest articleRequest, SemaphoreSlim semaphore,
        BatchImageRequest batchRequest)
    {
        await semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            return await ProcessSingleArticleAsync(articleRequest, batchRequest).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<ArticleImageResponse> ProcessSingleArticleAsync(ArticleImageRequest articleRequest, BatchImageRequest batchRequest)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = InitializeArticleResponse(articleRequest);

        try
        {
            using var httpClient = CreateHttpClient(batchRequest);
            await ProcessArticleContentAsync(httpClient, articleRequest, batchRequest, response, stopwatch).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            HandleHttpRequestException(response, articleRequest, ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            HandleTimeoutException(response, articleRequest, batchRequest);
        }
        catch (Exception ex)
        {
            HandleGeneralException(response, articleRequest, ex);
        }

        stopwatch.Stop();
        response.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;
        return response;
    }

    private static ArticleImageResponse InitializeArticleResponse(ArticleImageRequest articleRequest)
    {
        return new ArticleImageResponse
        {
            ArticleUrl = articleRequest.ArticleUrl,
            ProcessedAt = DateTime.UtcNow
        };
    }

    private HttpClient CreateHttpClient(BatchImageRequest batchRequest)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromMilliseconds(batchRequest.ArticleTimeoutMs);
        httpClient.DefaultRequestHeaders.Add("User-Agent", batchRequest.UserAgent);
        return httpClient;
    }

    private async Task ProcessArticleContentAsync(HttpClient httpClient, ArticleImageRequest articleRequest,
        BatchImageRequest batchRequest, ArticleImageResponse response, Stopwatch stopwatch)
    {
        var htmlContent = await FetchHtmlContentWithRetryAsync(httpClient, articleRequest.ArticleUrl).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            response.ErrorMessage = "Failed to fetch HTML content or content is empty";
            return;
        }

        await ExtractAndSetImagesAsync(htmlContent, articleRequest, batchRequest, response).ConfigureAwait(false);
        LogArticleProcessingSuccess(_logger, response.ExtractedImages.Count, articleRequest.ArticleUrl, (int)stopwatch.ElapsedMilliseconds);
    }

    private static async Task ExtractAndSetImagesAsync(string htmlContent, ArticleImageRequest articleRequest,
        BatchImageRequest batchRequest, ArticleImageResponse response)
    {
        var extractedImages = await ExtractImagesFromHtmlAsync(htmlContent, articleRequest.ArticleUrl).ConfigureAwait(false);
        response.ExtractedImages = extractedImages.Take(batchRequest.MaxImagesPerArticle).ToList();

        var primaryImage = response.ExtractedImages.OrderBy(i => i.Priority).FirstOrDefault();
        if (primaryImage != null)
        {
            response.PrimaryImageUrl = primaryImage.ImageUrl;
            response.PrimaryImageSource = primaryImage.Source;
            response.IsSuccess = true;
        }
        else
        {
            response.ErrorMessage = "No images found in the article";
        }
    }

    private void HandleHttpRequestException(ArticleImageResponse response, ArticleImageRequest articleRequest, HttpRequestException ex)
    {
        response.ErrorMessage = $"HTTP request failed: {ex.Message}";
        LogArticleProcessingFailed(_logger, articleRequest.ArticleUrl, ex.Message);
    }

    private void HandleTimeoutException(ArticleImageResponse response, ArticleImageRequest articleRequest, BatchImageRequest batchRequest)
    {
        response.ErrorMessage = $"Request timed out after {batchRequest.ArticleTimeoutMs.ToString(CultureInfo.InvariantCulture)}ms";
        LogArticleProcessingFailed(_logger, articleRequest.ArticleUrl, response.ErrorMessage);
    }

    private void HandleGeneralException(ArticleImageResponse response, ArticleImageRequest articleRequest, Exception ex)
    {
        response.ErrorMessage = $"Unexpected error: {ex.Message}";
        LogArticleProcessingFailed(_logger, articleRequest.ArticleUrl, ex.Message);
    }

    private async Task<string?> FetchHtmlContentAsync(HttpClient httpClient, string url)
    {
        try
        {
            var response = await httpClient.GetAsync(url).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                LogHttpRequestFailed(_logger, url, response.StatusCode, response.ReasonPhrase ?? "Unknown error");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return content;
        }
        catch (Exception ex)
        {
            LogHttpRequestFailed(_logger, url, HttpStatusCode.InternalServerError, ex.Message);
            return null;
        }
    }

    private static async Task<List<ExtractedImage>> ExtractImagesFromHtmlAsync(string htmlContent, string baseUrl)
    {
        var extractedImages = new List<ExtractedImage>();

        // Create AngleSharp configuration
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);

        // Parse the HTML document
        var document = await context.OpenAsync(req => req.Content(htmlContent)).ConfigureAwait(false);

        // Cast to IHtmlDocument for HTML-specific functionality
        if (document is IHtmlDocument htmlDocument)
        {
            // Extract Open Graph images (highest priority)
            ExtractOpenGraphImages(htmlDocument, extractedImages, baseUrl);

            // Extract Twitter Card images
            ExtractTwitterCardImages(htmlDocument, extractedImages, baseUrl);

            // Extract Apple Touch Icons
            ExtractAppleTouchIcons(htmlDocument, extractedImages, baseUrl);

            // Extract favicon
            ExtractFavicons(htmlDocument, extractedImages, baseUrl);

            // Extract other meta images
            ExtractGenericMetaImages(htmlDocument, extractedImages, baseUrl);
        }

        return extractedImages;
    }

    private static void ExtractOpenGraphImages(IHtmlDocument document, List<ExtractedImage> extractedImages, string baseUrl)
    {
        // Define Open Graph image properties in priority order
        var ogImageProperties = new[]
        {
            "og:image", // Primary Open Graph image (highest priority)
            "og:image:url", // Alternative URL property
            "og:image:secure_url" // Secure URL variant,
        };

        var priority = 1;

        // Extract images based on priority order
        foreach (var property in ogImageProperties)
        {
            var elements = document.QuerySelectorAll($"meta[property='{property}']");

            foreach (var element in elements)
            {
                var imageUrl = element.GetAttribute("content");
                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    var absoluteUrl = ConvertToAbsoluteUrl(imageUrl, baseUrl);
                    if (!string.IsNullOrWhiteSpace(absoluteUrl))
                        // Check if this URL is already added to avoid duplicates
                        if (!extractedImages.Any(img =>
                                string.Equals(img.ImageUrl, absoluteUrl, StringComparison.Ordinal) && img.Source == ImageSource.OpenGraph))
                        {
                            var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                            {
                                { "property", property },
                                { "content", imageUrl }
                            };

                            // Add related metadata if available
                            AddOpenGraphMetadata(document, metadata, absoluteUrl);

                            extractedImages.Add(new ExtractedImage
                            {
                                ImageUrl = absoluteUrl,
                                Source = ImageSource.OpenGraph,
                                Priority = priority++,
                                Metadata = metadata
                            });
                        }
                }
            }
        }
    }

    /// <summary>
    ///     Adds related Open Graph metadata for an image URL.
    /// </summary>
    private static void AddOpenGraphMetadata(IHtmlDocument document, Dictionary<string, string> metadata, string imageUrl)
    {
        // Try to find width and height for this specific image
        var widthElement = document.QuerySelector("meta[property='og:image:width'][content]");
        var heightElement = document.QuerySelector("meta[property='og:image:height'][content]");
        var altElement = document.QuerySelector("meta[property='og:image:alt'][content]");
        var typeElement = document.QuerySelector("meta[property='og:image:type'][content]");

        if (widthElement != null) metadata["width"] = widthElement.GetAttribute("content") ?? string.Empty;

        if (heightElement != null) metadata["height"] = heightElement.GetAttribute("content") ?? string.Empty;

        if (altElement != null) metadata["alt"] = altElement.GetAttribute("content") ?? string.Empty;

        if (typeElement != null) metadata["type"] = typeElement.GetAttribute("content") ?? string.Empty;
    }

    private static void ExtractTwitterCardImages(IHtmlDocument document, List<ExtractedImage> extractedImages, string baseUrl)
    {
        var twitterImageElements = document.QuerySelectorAll("meta[name^='twitter:image']");
        var priority = 100;

        foreach (var element in twitterImageElements)
        {
            var imageUrl = element.GetAttribute("content");
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                var absoluteUrl = ConvertToAbsoluteUrl(imageUrl, baseUrl);
                if (!string.IsNullOrWhiteSpace(absoluteUrl))
                    extractedImages.Add(new ExtractedImage
                    {
                        ImageUrl = absoluteUrl,
                        Source = ImageSource.Twitter,
                        Priority = priority++,
                        Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            { "name", element.GetAttribute("name") ?? string.Empty },
                            { "content", imageUrl }
                        }
                    });
            }
        }
    }

    private static void ExtractAppleTouchIcons(IHtmlDocument document, List<ExtractedImage> extractedImages, string baseUrl)
    {
        var appleIconElements = document.QuerySelectorAll("link[rel*='apple-touch-icon']");
        var priority = 200;

        foreach (var element in appleIconElements)
        {
            var imageUrl = element.GetAttribute("href");
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                var absoluteUrl = ConvertToAbsoluteUrl(imageUrl, baseUrl);
                if (!string.IsNullOrWhiteSpace(absoluteUrl))
                {
                    var sizes = element.GetAttribute("sizes") ?? string.Empty;
                    extractedImages.Add(new ExtractedImage
                    {
                        ImageUrl = absoluteUrl,
                        Source = ImageSource.Apple,
                        Priority = priority++,
                        Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            { "rel", element.GetAttribute("rel") ?? string.Empty },
                            { "sizes", sizes },
                            { "href", imageUrl }
                        }
                    });
                }
            }
        }
    }

    private static void ExtractFavicons(IHtmlDocument document, List<ExtractedImage> extractedImages, string baseUrl)
    {
        var faviconElements = document.QuerySelectorAll("link[rel*='icon']");
        var priority = 300;

        foreach (var element in faviconElements)
        {
            var imageUrl = element.GetAttribute("href");
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                var absoluteUrl = ConvertToAbsoluteUrl(imageUrl, baseUrl);
                if (!string.IsNullOrWhiteSpace(absoluteUrl))
                    extractedImages.Add(new ExtractedImage
                    {
                        ImageUrl = absoluteUrl,
                        Source = ImageSource.Favicon,
                        Priority = priority++,
                        Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            { "rel", element.GetAttribute("rel") ?? string.Empty },
                            { "type", element.GetAttribute("type") ?? string.Empty },
                            { "href", imageUrl }
                        }
                    });
            }
        }
    }

    private static void ExtractGenericMetaImages(IHtmlDocument document, List<ExtractedImage> extractedImages, string baseUrl)
    {
        var genericImageElements = document.QuerySelectorAll("meta[name='image'], meta[property='image']");
        var priority = 400;

        foreach (var element in genericImageElements)
        {
            var imageUrl = element.GetAttribute("content");
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                var absoluteUrl = ConvertToAbsoluteUrl(imageUrl, baseUrl);
                if (!string.IsNullOrWhiteSpace(absoluteUrl))
                    extractedImages.Add(new ExtractedImage
                    {
                        ImageUrl = absoluteUrl,
                        Source = ImageSource.Generic,
                        Priority = priority++,
                        Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            { "name", element.GetAttribute("name") ?? string.Empty },
                            { "property", element.GetAttribute("property") ?? string.Empty },
                            { "content", imageUrl }
                        }
                    });
            }
        }
    }

    /// <summary>
    ///     Fetches HTML content with comprehensive error handling, rate limiting, and retry logic.
    /// </summary>
    private async Task<string?> FetchHtmlContentWithRetryAsync(HttpClient httpClient, string url, int maxRetries = 3)
    {
        var domain = GetDomainFromUrl(url);
        if (string.IsNullOrEmpty(domain)) return null;

        if (!await CanMakeRequestAsync(domain).ConfigureAwait(false)) return null;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            if (attempt > 1) await DelayBeforeRetryAsync(url, attempt, maxRetries).ConfigureAwait(false);

            var result = await TryFetchHtmlContentAsync(httpClient, url, domain, attempt, maxRetries).ConfigureAwait(false);
            if (result != null || string.IsNullOrEmpty(result)) return string.IsNullOrEmpty(result) ? null : result;
        }

        return null;
    }

    private async Task<bool> CanMakeRequestAsync(string domain)
    {
        if (IsCircuitBreakerOpen(domain)) return false;
        return await CheckRateLimitAsync(domain).ConfigureAwait(false);
    }

    private async Task DelayBeforeRetryAsync(string url, int attempt, int maxRetries)
    {
        LogRequestRetry(_logger, url, attempt, maxRetries);
        await Task.Delay(GetRetryDelay(attempt)).ConfigureAwait(false);
    }

    private async Task<string?> TryFetchHtmlContentAsync(HttpClient httpClient, string url, string domain, int attempt, int maxRetries)
    {
        try
        {
            var response = await httpClient.GetAsync(url).ConfigureAwait(false);
            return await HandleHttpResponseAsync(response, url, domain, attempt, maxRetries).ConfigureAwait(false);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            return HandleTimeoutException(url, domain, attempt, maxRetries);
        }
        catch (HttpRequestException ex)
        {
            return HandleHttpRequestException(url, domain, attempt, maxRetries, ex);
        }
        catch (Exception ex)
        {
            return HandleGeneralException(url, domain, attempt, maxRetries, ex);
        }
    }

    private async Task<string?> HandleHttpResponseAsync(HttpResponseMessage response, string url, string domain, int attempt, int maxRetries)
    {
        if (response.IsSuccessStatusCode)
        {
            ResetCircuitBreaker(domain);
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        var shouldRetry = ShouldRetryBasedOnStatusCode(response.StatusCode);
        if (!shouldRetry || attempt == maxRetries)
        {
            LogHttpRequestFailed(_logger, url, response.StatusCode, response.ReasonPhrase ?? "Unknown error");
            if (IsServerError(response.StatusCode)) IncrementCircuitBreakerFailures(domain);
            return string.Empty;
        }

        return null;
    }

    private string? HandleTimeoutException(string url, string domain, int attempt, int maxRetries)
    {
        if (attempt == maxRetries)
        {
            LogHttpRequestFailed(_logger, url, HttpStatusCode.RequestTimeout, "Request timed out");
            IncrementCircuitBreakerFailures(domain);
            return string.Empty;
        }

        return null;
    }

    private string? HandleHttpRequestException(string url, string domain, int attempt, int maxRetries, HttpRequestException ex)
    {
        if (attempt == maxRetries)
        {
            LogHttpRequestFailed(_logger, url, HttpStatusCode.InternalServerError, ex.Message);
            IncrementCircuitBreakerFailures(domain);
            return string.Empty;
        }

        return null;
    }

    private string? HandleGeneralException(string url, string domain, int attempt, int maxRetries, Exception ex)
    {
        if (attempt == maxRetries)
        {
            LogHttpRequestFailed(_logger, url, HttpStatusCode.InternalServerError, ex.Message);
            IncrementCircuitBreakerFailures(domain);
            return string.Empty;
        }

        return null;
    }

    /// <summary>
    ///     Checks if the request should be rate limited for the given domain.
    /// </summary>
    private Task<bool> CheckRateLimitAsync(string domain)
    {
        const int maxRequestsPerMinute = 60;
        const int timeWindowSeconds = 60;

        var tracker = RateLimitTrackers.GetOrAdd(domain, _ => new RateLimitTracker());
        var now = DateTime.UtcNow;

        // Clean up old entries
        tracker.RequestTimes.RemoveAll(t => (now - t).TotalSeconds > timeWindowSeconds);

        if (tracker.RequestTimes.Count >= maxRequestsPerMinute)
        {
            LogRateLimitExceeded(_logger, domain, tracker.RequestTimes.Count, timeWindowSeconds);
            return Task.FromResult(false);
        }

        tracker.RequestTimes.Add(now);
        return Task.FromResult(true);
    }

    /// <summary>
    ///     Checks if the circuit breaker is open for the given domain.
    /// </summary>
    private bool IsCircuitBreakerOpen(string domain)
    {
        if (!CircuitBreakers.TryGetValue(domain, out var state)) return false;

        if (state.IsOpen)
        {
            // Check if we should attempt to close the circuit breaker
            // 5-minute timeout
            if (DateTime.UtcNow > state.OpenedAt.AddMinutes(5))
            {
                state.IsOpen = false;
                state.FailureCount = 0;
                LogCircuitBreakerClosed(_logger, domain);
                return false;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Increments the circuit breaker failure count and opens it if threshold is reached.
    /// </summary>
    private void IncrementCircuitBreakerFailures(string domain)
    {
        const int failureThreshold = 5;

        var state = CircuitBreakers.GetOrAdd(domain, _ => new CircuitBreakerState());
        state.FailureCount++;

        if (state.FailureCount >= failureThreshold && !state.IsOpen)
        {
            state.IsOpen = true;
            state.OpenedAt = DateTime.UtcNow;
            LogCircuitBreakerOpened(_logger, domain, state.FailureCount);
        }
    }

    /// <summary>
    ///     Resets the circuit breaker for the given domain.
    /// </summary>
    private void ResetCircuitBreaker(string domain)
    {
        if (CircuitBreakers.TryGetValue(domain, out var state))
        {
            state.FailureCount = 0;
            if (state.IsOpen)
            {
                state.IsOpen = false;
                LogCircuitBreakerClosed(_logger, domain);
            }
        }
    }

    /// <summary>
    ///     Determines if a request should be retried based on the HTTP status code.
    /// </summary>
    private static bool ShouldRetryBasedOnStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.RequestTimeout => true,
            HttpStatusCode.TooManyRequests => true,
            HttpStatusCode.InternalServerError => true,
            HttpStatusCode.BadGateway => true,
            HttpStatusCode.ServiceUnavailable => true,
            HttpStatusCode.GatewayTimeout => true,
            _ => false
        };
    }

    /// <summary>
    ///     Determines if the status code indicates a server error.
    /// </summary>
    private static bool IsServerError(HttpStatusCode statusCode)
    {
        return (int)statusCode >= 500;
    }

    /// <summary>
    ///     Calculates the retry delay using exponential backoff.
    /// </summary>
    private static int GetRetryDelay(int attempt)
    {
        // 1 second base delay
        var baseDelay = 1000;
        var delay = baseDelay * Math.Pow(2, attempt - 1);
        // Add jitter to prevent thundering herd
        var jitter = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 500);
        // Max 30 seconds
        return (int)Math.Min(delay + jitter, 30000);
    }

    /// <summary>
    ///     Extracts the domain from a URL.
    /// </summary>
    private static string? GetDomainFromUrl(string url)
    {
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri)) return uri.Host;
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }

    private static string? ConvertToAbsoluteUrl(string imageUrl, string baseUrl)
    {
        try
        {
            if (Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute)) return imageUrl;

            if (Uri.TryCreate(new Uri(baseUrl), imageUrl, out var absoluteUri)) return absoluteUri.ToString();

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Reads and validates the request body with size limits.
    /// </summary>
    private async Task<string?> ReadRequestBodyWithValidationAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        const int maxRequestSizeBytes = 1024 * 1024; // 1MB limit

        try
        {
            using var reader = new StreamReader(request.Body);
            var requestBody = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(requestBody)) return null;

            // Check request size
            var requestSizeBytes = Encoding.UTF8.GetByteCount(requestBody);
            var requestSizeKB = requestSizeBytes / 1024;

            if (requestSizeBytes > maxRequestSizeBytes)
            {
                LogRequestSizeValidation(_logger, -1, requestSizeKB);
                return null;
            }

            LogRequestSizeValidation(_logger, -1, requestSizeKB);
            return requestBody;
        }
        catch (Exception ex)
        {
            LogRequestReadBodyFailed(_logger, ex, ex.Message);
            return null;
        }
    }

    /// <summary>
    ///     Validates the batch request for required fields, limits, and security.
    /// </summary>
    private static ValidationResult ValidateBatchRequest(BatchImageRequest request)
    {
        var result = new ValidationResult { IsValid = true };

        if (!ValidateBasicRequirements(request, result)) return result;

        ValidateRequestLimits(request, result);
        ValidateTimeoutValues(request, result);
        ValidateRequestMetadata(request, result);
        ValidateArticles(request, result);

        if (result.Errors.Count > 0) result.IsValid = false;
        return result;
    }

    private static bool ValidateBasicRequirements(BatchImageRequest request, ValidationResult result)
    {
        if (request.Articles == null || request.Articles.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add("No articles provided for processing");
            return false;
        }

        return true;
    }

    private static void ValidateRequestLimits(BatchImageRequest request, ValidationResult result)
    {
        if (request.Articles.Count > 100)
        {
            result.IsValid = false;
            result.Errors.Add("Maximum 100 articles allowed per batch request");
        }

        if (request.MaxConcurrency <= 0 || request.MaxConcurrency > 20)
        {
            result.IsValid = false;
            result.Errors.Add("MaxConcurrency must be between 1 and 20");
        }

        if (request.MaxImagesPerArticle <= 0 || request.MaxImagesPerArticle > 10)
        {
            result.IsValid = false;
            result.Errors.Add("MaxImagesPerArticle must be between 1 and 10");
        }
    }

    private static void ValidateTimeoutValues(BatchImageRequest request, ValidationResult result)
    {
        // 10 seconds to 10 minutes
        if (request.BatchTimeoutMs < 10000 || request.BatchTimeoutMs > 600000)
        {
            result.IsValid = false;
            result.Errors.Add("BatchTimeoutMs must be between 10,000 and 600,000 milliseconds");
        }

        // 5 seconds to 2 minutes
        if (request.ArticleTimeoutMs < 5000 || request.ArticleTimeoutMs > 120000)
        {
            result.IsValid = false;
            result.Errors.Add("ArticleTimeoutMs must be between 5,000 and 120,000 milliseconds");
        }
    }

    private static void ValidateRequestMetadata(BatchImageRequest request, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(request.RequestId) || request.RequestId.Length > 100)
        {
            result.IsValid = false;
            result.Errors.Add("RequestId must be provided and cannot exceed 100 characters");
        }

        if (string.IsNullOrWhiteSpace(request.UserAgent) || request.UserAgent.Length > 200)
        {
            result.IsValid = false;
            result.Errors.Add("UserAgent must be provided and cannot exceed 200 characters");
        }
    }

    private static void ValidateArticles(BatchImageRequest request, ValidationResult result)
    {
        for (var i = 0; i < request.Articles.Count; i++)
        {
            var article = request.Articles[i];
            var articleErrors = ValidateArticleRequest(article, i);
            result.Errors.AddRange(articleErrors);
        }
    }

    /// <summary>
    ///     Validates an individual article request.
    /// </summary>
    private static List<string> ValidateArticleRequest(ArticleImageRequest article, int index)
    {
        var errors = new List<string>();

        // Validate ArticleUrl
        if (string.IsNullOrWhiteSpace(article.ArticleUrl))
        {
            errors.Add($"Article[{index.ToString(CultureInfo.InvariantCulture)}]: ArticleUrl is required");
        }
        else
        {
            // Validate URL format and security
            if (!Uri.TryCreate(article.ArticleUrl, UriKind.Absolute, out var uri))
            {
                errors.Add($"Article[{index.ToString(CultureInfo.InvariantCulture)}]: ArticleUrl is not a valid URL");
            }
            else
            {
                // Security validation: only allow HTTP/HTTPS
                if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.InvariantCultureIgnoreCase) && !string.Equals(uri.Scheme, Uri.UriSchemeHttps,
                        StringComparison.Ordinal)) errors.Add($"Article[{index.ToString(CultureInfo.InvariantCulture)}]: Only HTTP and HTTPS URLs are allowed");

                // Block potentially dangerous domains
                var host = uri.Host.ToLowerInvariant();
                if (string.Equals(host, "localhost", StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(host, "127.0.0.1", StringComparison.Ordinal) ||
                    host.StartsWith("192.168.", StringComparison.Ordinal) ||
                    host.StartsWith("10.", StringComparison.Ordinal))
                    errors.Add($"Article[{index.ToString(CultureInfo.InvariantCulture)}]: Internal/local network URLs are not allowed");

                // URL length validation
                if (article.ArticleUrl.Length > 2000) errors.Add($"Article[{index.ToString(CultureInfo.InvariantCulture)}]: ArticleUrl cannot exceed 2000 characters");
            }
        }

        // Validate optional fields
        if (article.ArticleTitle != null && article.ArticleTitle.Length > 500) errors.Add($"Article[{index.ToString(CultureInfo.InvariantCulture)}]: ArticleTitle cannot exceed 500 characters");

        if (article.ArticleDescription != null && article.ArticleDescription.Length > 1000)
            errors.Add($"Article[{index.ToString(CultureInfo.InvariantCulture)}]: ArticleDescription cannot exceed 1000 characters");

        if (article.UserAgent != null && article.UserAgent.Length > 200) errors.Add($"Article[{index.ToString(CultureInfo.InvariantCulture)}]: UserAgent cannot exceed 200 characters");

        // Validate timeout values
        if (article.TimeoutMs < 5000 || article.TimeoutMs > 120000) errors.Add($"Article[{index.ToString(CultureInfo.InvariantCulture)}]: TimeoutMs must be between 5,000 and 120,000 milliseconds");

        if (article.MaxImages <= 0 || article.MaxImages > 10) errors.Add($"Article[{index.ToString(CultureInfo.InvariantCulture)}]: MaxImages must be between 1 and 10");

        return errors;
    }

    /// <summary>
    ///     Tracks rate limiting for a domain.
    /// </summary>
    private sealed class RateLimitTracker
    {
        public List<DateTime> RequestTimes { get; } = new();
    }

    /// <summary>
    ///     Tracks circuit breaker state for a domain.
    /// </summary>
    private sealed class CircuitBreakerState
    {
        public int FailureCount { get; set; }
        public bool IsOpen { get; set; }
        public DateTime OpenedAt { get; set; }
    }

    /// <summary>
    ///     Represents the result of request validation.
    /// </summary>
    private sealed class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; } = new();
    }
}