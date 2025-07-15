using System.Diagnostics;
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
using System.Collections.Concurrent;
using System.Threading.RateLimiting;

namespace redmuffin.Blazor.StaticWeb.Api.Functions;

/// <summary>
/// Azure Function for batch Open Graph image processing with HTML parsing using AngleSharp.
/// </summary>
public partial class GetOpenGraphImages(ILogger<GetOpenGraphImages> logger, IHttpClientFactory httpClientFactory)
{
    private readonly ILogger<GetOpenGraphImages> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

    [LoggerMessage(1, LogLevel.Information, "Processing batch request {RequestId} with {ArticleCount} articles", EventName = "BatchProcessing_Started")]
    public static partial void LogBatchProcessingStarted(ILogger logger, string requestId, int articleCount);

    [LoggerMessage(2, LogLevel.Information, "Batch request {RequestId} completed in {ElapsedMs}ms - Success: {SuccessCount}, Failed: {FailureCount}", EventName = "BatchProcessing_Completed")]
    public static partial void LogBatchProcessingCompleted(ILogger logger, string requestId, int elapsedMs, int successCount, int failureCount);

    [LoggerMessage(3, LogLevel.Warning, "Failed to process article {ArticleUrl}: {ErrorMessage}", EventName = "Article_ProcessingFailed")]
    public static partial void LogArticleProcessingFailed(ILogger logger, string articleUrl, string errorMessage);

    [LoggerMessage(4, LogLevel.Debug, "Extracted {ImageCount} images from {ArticleUrl} in {ElapsedMs}ms", EventName = "Article_ProcessingSuccess")]
    public static partial void LogArticleProcessingSuccess(ILogger logger, int imageCount, string articleUrl, int elapsedMs);

    [LoggerMessage(5, LogLevel.Error, "HTTP request failed for {ArticleUrl}: {StatusCode} - {ErrorMessage}", EventName = "Http_RequestFailed")]
    public static partial void LogHttpRequestFailed(ILogger logger, string articleUrl, HttpStatusCode statusCode, string errorMessage);

    [LoggerMessage(6, LogLevel.Information, "Starting parallel processing of {ArticleCount} articles with max concurrency {MaxConcurrency}", EventName = "ParallelProcessing_Started")]
    public static partial void LogParallelProcessingStarted(ILogger logger, int articleCount, int maxConcurrency);

    [LoggerMessage(7, LogLevel.Information, "Parallel processing completed - {ConcurrentTasks} concurrent tasks, peak memory: {PeakMemoryMB}MB", EventName = "ParallelProcessing_Completed")]
    public static partial void LogParallelProcessingCompleted(ILogger logger, int concurrentTasks, long peakMemoryMB);

    [LoggerMessage(8, LogLevel.Warning, "Rate limit exceeded for domain {Domain}. Requests: {RequestCount}, Time window: {TimeWindowSeconds}s", EventName = "RateLimit_Exceeded")]
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

    [LoggerMessage(14, LogLevel.Information, "Request size validation - Articles: {ArticleCount}, Request size: {RequestSizeKB}KB", EventName = "Request_SizeValidation")]
    public static partial void LogRequestSizeValidation(ILogger logger, int articleCount, int requestSizeKB);

    [Function("GetOpenGraphImages")]
    public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Add request timeout at the function level
            using var functionCts = new CancellationTokenSource(TimeSpan.FromMinutes(10)); // Maximum function timeout
            
            // Read and deserialize the request body with size validation
            var requestBody = await ReadRequestBodyWithValidationAsync(request, functionCts.Token);
            if (requestBody == null)
            {
                return new BadRequestObjectResult("Request body is empty or too large");
            }

            var batchRequest = JsonSerializer.Deserialize<BatchImageRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (batchRequest == null)
            {
                return new BadRequestObjectResult("Invalid request format");
            }

            // Comprehensive request validation
            var validationResult = ValidateBatchRequest(batchRequest);
            if (!validationResult.IsValid)
            {
                LogRequestValidationFailed(_logger, string.Join(", ", validationResult.Errors));
                return new BadRequestObjectResult(new { 
                    Error = "Request validation failed", 
                    ValidationErrors = validationResult.Errors 
                });
            }

            LogBatchProcessingStarted(_logger, batchRequest.RequestId, batchRequest.Articles.Count);

            // Process the batch request
            var batchResponse = await ProcessBatchRequestAsync(batchRequest);

            stopwatch.Stop();
            LogBatchProcessingCompleted(_logger, batchRequest.RequestId, (int)stopwatch.ElapsedMilliseconds, 
                batchResponse.SuccessCount, batchResponse.FailureCount);

            batchResponse.TotalProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;

            return new OkObjectResult(batchResponse);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize batch request");
            return new BadRequestObjectResult("Invalid JSON format");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing batch request");
            return new StatusCodeResult(500);
        }
    }

    private async Task<BatchImageResponse> ProcessBatchRequestAsync(BatchImageRequest batchRequest)
    {
        var startTime = DateTime.UtcNow;
        var initialMemory = GC.GetTotalMemory(false);
        
        var response = new BatchImageResponse
        {
            RequestId = batchRequest.RequestId,
            StartedAt = startTime,
            TotalProcessed = batchRequest.Articles.Count
        };

        // Create semaphore for concurrency control
        using var semaphore = new SemaphoreSlim(batchRequest.MaxConcurrency, batchRequest.MaxConcurrency);

        // Log parallel processing start
        LogParallelProcessingStarted(_logger, batchRequest.Articles.Count, batchRequest.MaxConcurrency);

        // Process articles in parallel with concurrency control
        var tasks = batchRequest.Articles.Select(article => ProcessArticleWithSemaphoreAsync(article, semaphore, batchRequest));
        
        // Track performance metrics
        var processingTimes = new List<int>();
        var httpRequestCount = 0;
        var validationRequestCount = 0;

        using var cts = new CancellationTokenSource(batchRequest.BatchTimeoutMs);
        
        try
        {
            // Wait for all tasks to complete with timeout
            var results = await Task.WhenAll(tasks).WaitAsync(cts.Token);

            response.Results = results.ToList();
            response.SuccessCount = results.Count(r => r.IsSuccess);
            response.FailureCount = results.Count(r => !r.IsSuccess);
            response.CacheHitCount = results.Count(r => r.FromCache);
            response.IsSuccess = response.FailureCount == 0 || !batchRequest.StopOnFirstError;

            // Capture peak memory usage
            var endMemory = GC.GetTotalMemory(false);
            var peakMemoryMB = (endMemory - initialMemory) / (1024 * 1024);

            // Log completion
            LogParallelProcessingCompleted(_logger, batchRequest.MaxConcurrency, peakMemoryMB);
        }
        catch (TimeoutException)
        {
            response.IsSuccess = false;
            response.ErrorMessages.Add($"Batch processing timed out after {batchRequest.BatchTimeoutMs}ms");
            LogRequestTimedOut(_logger, batchRequest.BatchTimeoutMs, batchRequest.RequestId);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            response.IsSuccess = false;
            response.ErrorMessages.Add($"Batch processing was cancelled due to timeout after {batchRequest.BatchTimeoutMs}ms");
            LogRequestTimedOut(_logger, batchRequest.BatchTimeoutMs, batchRequest.RequestId);
        }
        catch (Exception ex)
        {
            response.IsSuccess = false;
            response.ErrorMessages.Add($"Batch processing failed: {ex.Message}");
        }

        response.ProcessedAt = DateTime.UtcNow;
        return response;
    }

    private async Task<ArticleImageResponse> ProcessArticleWithSemaphoreAsync(ArticleImageRequest articleRequest, SemaphoreSlim semaphore, BatchImageRequest batchRequest)
    {
        await semaphore.WaitAsync();
        try
        {
            return await ProcessSingleArticleAsync(articleRequest, batchRequest);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<ArticleImageResponse> ProcessSingleArticleAsync(ArticleImageRequest articleRequest, BatchImageRequest batchRequest)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new ArticleImageResponse
        {
            ArticleUrl = articleRequest.ArticleUrl,
            ProcessedAt = DateTime.UtcNow
        };

        try
        {
            // Create HTTP client with timeout
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromMilliseconds(batchRequest.ArticleTimeoutMs);
            httpClient.DefaultRequestHeaders.Add("User-Agent", batchRequest.UserAgent);

            // Fetch the HTML content with comprehensive error handling
            var htmlContent = await FetchHtmlContentWithRetryAsync(httpClient, articleRequest.ArticleUrl);
            if (string.IsNullOrWhiteSpace(htmlContent))
            {
                response.ErrorMessage = "Failed to fetch HTML content or content is empty";
                return response;
            }

            // Parse HTML and extract images
            var extractedImages = await ExtractImagesFromHtmlAsync(htmlContent, articleRequest.ArticleUrl);
            response.ExtractedImages = extractedImages.Take(batchRequest.MaxImagesPerArticle).ToList();

            // Set primary image (highest priority)
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

            LogArticleProcessingSuccess(_logger, response.ExtractedImages.Count, articleRequest.ArticleUrl, (int)stopwatch.ElapsedMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            response.ErrorMessage = $"HTTP request failed: {ex.Message}";
            LogArticleProcessingFailed(_logger, articleRequest.ArticleUrl, ex.Message);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            response.ErrorMessage = $"Request timed out after {batchRequest.ArticleTimeoutMs}ms";
            LogArticleProcessingFailed(_logger, articleRequest.ArticleUrl, response.ErrorMessage);
        }
        catch (Exception ex)
        {
            response.ErrorMessage = $"Unexpected error: {ex.Message}";
            LogArticleProcessingFailed(_logger, articleRequest.ArticleUrl, ex.Message);
        }

        stopwatch.Stop();
        response.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;
        return response;
    }

    private async Task<string?> FetchHtmlContentAsync(HttpClient httpClient, string url)
    {
        try
        {
            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                LogHttpRequestFailed(_logger, url, response.StatusCode, response.ReasonPhrase ?? "Unknown error");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return content;
        }
        catch (Exception ex)
        {
            LogHttpRequestFailed(_logger, url, HttpStatusCode.InternalServerError, ex.Message);
            return null;
        }
    }

    private async Task<List<ExtractedImage>> ExtractImagesFromHtmlAsync(string htmlContent, string baseUrl)
    {
        var extractedImages = new List<ExtractedImage>();
        
        // Create AngleSharp configuration
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        
        // Parse the HTML document
        var document = await context.OpenAsync(req => req.Content(htmlContent));
        
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

    private void ExtractOpenGraphImages(IHtmlDocument document, List<ExtractedImage> extractedImages, string baseUrl)
    {
        // Define Open Graph image properties in priority order
        var ogImageProperties = new string[]
        {
            "og:image",           // Primary Open Graph image (highest priority)
            "og:image:url",      // Alternative URL property
            "og:image:secure_url" // Secure URL variant
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
                    {
                        // Check if this URL is already added to avoid duplicates
                        if (!extractedImages.Any(img => img.ImageUrl == absoluteUrl && img.Source == ImageSource.OpenGraph))
                        {
                            var metadata = new Dictionary<string, string>
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
    }

    /// <summary>
    /// Adds related Open Graph metadata for an image URL.
    /// </summary>
    private void AddOpenGraphMetadata(IHtmlDocument document, Dictionary<string, string> metadata, string imageUrl)
    {
        // Try to find width and height for this specific image
        var widthElement = document.QuerySelector($"meta[property='og:image:width'][content]");
        var heightElement = document.QuerySelector($"meta[property='og:image:height'][content]");
        var altElement = document.QuerySelector($"meta[property='og:image:alt'][content]");
        var typeElement = document.QuerySelector($"meta[property='og:image:type'][content]");

        if (widthElement != null)
        {
            metadata["width"] = widthElement.GetAttribute("content") ?? string.Empty;
        }

        if (heightElement != null)
        {
            metadata["height"] = heightElement.GetAttribute("content") ?? string.Empty;
        }

        if (altElement != null)
        {
            metadata["alt"] = altElement.GetAttribute("content") ?? string.Empty;
        }

        if (typeElement != null)
        {
            metadata["type"] = typeElement.GetAttribute("content") ?? string.Empty;
        }
    }

    private void ExtractTwitterCardImages(IHtmlDocument document, List<ExtractedImage> extractedImages, string baseUrl)
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
                {
                    extractedImages.Add(new ExtractedImage
                    {
                        ImageUrl = absoluteUrl,
                        Source = ImageSource.Twitter,
                        Priority = priority++,
                        Metadata = new Dictionary<string, string>
                        {
                            { "name", element.GetAttribute("name") ?? string.Empty },
                            { "content", imageUrl }
                        }
                    });
                }
            }
        }
    }

    private void ExtractAppleTouchIcons(IHtmlDocument document, List<ExtractedImage> extractedImages, string baseUrl)
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
                        Metadata = new Dictionary<string, string>
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

    private void ExtractFavicons(IHtmlDocument document, List<ExtractedImage> extractedImages, string baseUrl)
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
                {
                    extractedImages.Add(new ExtractedImage
                    {
                        ImageUrl = absoluteUrl,
                        Source = ImageSource.Favicon,
                        Priority = priority++,
                        Metadata = new Dictionary<string, string>
                        {
                            { "rel", element.GetAttribute("rel") ?? string.Empty },
                            { "type", element.GetAttribute("type") ?? string.Empty },
                            { "href", imageUrl }
                        }
                    });
                }
            }
        }
    }

    private void ExtractGenericMetaImages(IHtmlDocument document, List<ExtractedImage> extractedImages, string baseUrl)
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
                {
                    extractedImages.Add(new ExtractedImage
                    {
                        ImageUrl = absoluteUrl,
                        Source = ImageSource.Generic,
                        Priority = priority++,
                        Metadata = new Dictionary<string, string>
                        {
                            { "name", element.GetAttribute("name") ?? string.Empty },
                            { "property", element.GetAttribute("property") ?? string.Empty },
                            { "content", imageUrl }
                        }
                    });
                }
            }
        }
    }

    // Static collections for rate limiting and circuit breaker
    private static readonly ConcurrentDictionary<string, RateLimitTracker> _rateLimitTrackers = new();
    private static readonly ConcurrentDictionary<string, CircuitBreakerState> _circuitBreakers = new();

    /// <summary>
    /// Fetches HTML content with comprehensive error handling, rate limiting, and retry logic.
    /// </summary>
    private async Task<string?> FetchHtmlContentWithRetryAsync(HttpClient httpClient, string url, int maxRetries = 3)
    {
        var domain = GetDomainFromUrl(url);
        if (string.IsNullOrEmpty(domain))
        {
            return null;
        }

        // Check circuit breaker
        if (IsCircuitBreakerOpen(domain))
        {
            return null;
        }

        // Check rate limiting
        if (!await CheckRateLimitAsync(domain))
        {
            return null;
        }

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (attempt > 1)
                {
                    LogRequestRetry(_logger, url, attempt, maxRetries);
                    await Task.Delay(GetRetryDelay(attempt));
                }

                var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    // Reset circuit breaker on success
                    ResetCircuitBreaker(domain);
                    return await response.Content.ReadAsStringAsync();
                }

                // Handle different HTTP status codes
                var shouldRetry = ShouldRetryBasedOnStatusCode(response.StatusCode);
                if (!shouldRetry || attempt == maxRetries)
                {
                    LogHttpRequestFailed(_logger, url, response.StatusCode, response.ReasonPhrase ?? "Unknown error");
                    
                    if (IsServerError(response.StatusCode))
                    {
                        IncrementCircuitBreakerFailures(domain);
                    }
                    
                    return null;
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                if (attempt == maxRetries)
                {
                    LogHttpRequestFailed(_logger, url, HttpStatusCode.RequestTimeout, "Request timed out");
                    IncrementCircuitBreakerFailures(domain);
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                if (attempt == maxRetries)
                {
                    LogHttpRequestFailed(_logger, url, HttpStatusCode.InternalServerError, ex.Message);
                    IncrementCircuitBreakerFailures(domain);
                    return null;
                }
            }
            catch (Exception ex)
            {
                if (attempt == maxRetries)
                {
                    LogHttpRequestFailed(_logger, url, HttpStatusCode.InternalServerError, ex.Message);
                    IncrementCircuitBreakerFailures(domain);
                    return null;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if the request should be rate limited for the given domain.
    /// </summary>
    private async Task<bool> CheckRateLimitAsync(string domain)
    {
        const int maxRequestsPerMinute = 60;
        const int timeWindowSeconds = 60;

        var tracker = _rateLimitTrackers.GetOrAdd(domain, _ => new RateLimitTracker());
        var now = DateTime.UtcNow;

        // Clean up old entries
        tracker.RequestTimes.RemoveAll(t => (now - t).TotalSeconds > timeWindowSeconds);

        if (tracker.RequestTimes.Count >= maxRequestsPerMinute)
        {
            LogRateLimitExceeded(_logger, domain, tracker.RequestTimes.Count, timeWindowSeconds);
            return false;
        }

        tracker.RequestTimes.Add(now);
        return true;
    }

    /// <summary>
    /// Checks if the circuit breaker is open for the given domain.
    /// </summary>
    private bool IsCircuitBreakerOpen(string domain)
    {
        if (!_circuitBreakers.TryGetValue(domain, out var state))
        {
            return false;
        }

        if (state.IsOpen)
        {
            // Check if we should attempt to close the circuit breaker
            if (DateTime.UtcNow > state.OpenedAt.AddMinutes(5)) // 5-minute timeout
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
    /// Increments the circuit breaker failure count and opens it if threshold is reached.
    /// </summary>
    private void IncrementCircuitBreakerFailures(string domain)
    {
        const int failureThreshold = 5;
        
        var state = _circuitBreakers.GetOrAdd(domain, _ => new CircuitBreakerState());
        state.FailureCount++;
        
        if (state.FailureCount >= failureThreshold && !state.IsOpen)
        {
            state.IsOpen = true;
            state.OpenedAt = DateTime.UtcNow;
            LogCircuitBreakerOpened(_logger, domain, state.FailureCount);
        }
    }

    /// <summary>
    /// Resets the circuit breaker for the given domain.
    /// </summary>
    private void ResetCircuitBreaker(string domain)
    {
        if (_circuitBreakers.TryGetValue(domain, out var state))
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
    /// Determines if a request should be retried based on the HTTP status code.
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
    /// Determines if the status code indicates a server error.
    /// </summary>
    private static bool IsServerError(HttpStatusCode statusCode)
    {
        return (int)statusCode >= 500;
    }

    /// <summary>
    /// Calculates the retry delay using exponential backoff.
    /// </summary>
    private static int GetRetryDelay(int attempt)
    {
        var baseDelay = 1000; // 1 second
        var delay = baseDelay * Math.Pow(2, attempt - 1);
        var jitter = new Random().Next(0, 500); // Add jitter to prevent thundering herd
        return (int)Math.Min(delay + jitter, 30000); // Max 30 seconds
    }

    /// <summary>
    /// Extracts the domain from a URL.
    /// </summary>
    private static string? GetDomainFromUrl(string url)
    {
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return uri.Host;
            }
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
            if (Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
            {
                return imageUrl;
            }

            if (Uri.TryCreate(new Uri(baseUrl), imageUrl, out var absoluteUri))
            {
                return absoluteUri.ToString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Tracks rate limiting for a domain.
    /// </summary>
    private class RateLimitTracker
    {
        public List<DateTime> RequestTimes { get; } = new();
    }

    /// <summary>
    /// Tracks circuit breaker state for a domain.
    /// </summary>
    private class CircuitBreakerState
    {
        public int FailureCount { get; set; }
        public bool IsOpen { get; set; }
        public DateTime OpenedAt { get; set; }
    }

    /// <summary>
    /// Represents the result of request validation.
    /// </summary>
    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Reads and validates the request body with size limits.
    /// </summary>
    private async Task<string?> ReadRequestBodyWithValidationAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        const int maxRequestSizeBytes = 1024 * 1024; // 1MB limit
        
        try
        {
            using var reader = new StreamReader(request.Body);
            var requestBody = await reader.ReadToEndAsync(cancellationToken);
            
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return null;
            }

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
            _logger.LogError(ex, "Failed to read request body");
            return null;
        }
    }

    /// <summary>
    /// Validates the batch request for required fields, limits, and security.
    /// </summary>
    private ValidationResult ValidateBatchRequest(BatchImageRequest request)
    {
        var result = new ValidationResult { IsValid = true };

        // Validate basic requirements
        if (request.Articles == null || request.Articles.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add("No articles provided for processing");
            return result;
        }

        // Validate request limits
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

        // Validate timeout values
        if (request.BatchTimeoutMs < 10000 || request.BatchTimeoutMs > 600000) // 10 seconds to 10 minutes
        {
            result.IsValid = false;
            result.Errors.Add("BatchTimeoutMs must be between 10,000 and 600,000 milliseconds");
        }

        if (request.ArticleTimeoutMs < 5000 || request.ArticleTimeoutMs > 120000) // 5 seconds to 2 minutes
        {
            result.IsValid = false;
            result.Errors.Add("ArticleTimeoutMs must be between 5,000 and 120,000 milliseconds");
        }

        if (request.MaxImagesPerArticle <= 0 || request.MaxImagesPerArticle > 10)
        {
            result.IsValid = false;
            result.Errors.Add("MaxImagesPerArticle must be between 1 and 10");
        }

        // Validate RequestId
        if (string.IsNullOrWhiteSpace(request.RequestId) || request.RequestId.Length > 100)
        {
            result.IsValid = false;
            result.Errors.Add("RequestId must be provided and cannot exceed 100 characters");
        }

        // Validate User-Agent
        if (string.IsNullOrWhiteSpace(request.UserAgent) || request.UserAgent.Length > 200)
        {
            result.IsValid = false;
            result.Errors.Add("UserAgent must be provided and cannot exceed 200 characters");
        }

        // Validate individual article requests
        for (int i = 0; i < request.Articles.Count; i++)
        {
            var article = request.Articles[i];
            var articleErrors = ValidateArticleRequest(article, i);
            result.Errors.AddRange(articleErrors);
        }

        if (result.Errors.Count > 0)
        {
            result.IsValid = false;
        }

        return result;
    }

    /// <summary>
    /// Validates an individual article request.
    /// </summary>
    private List<string> ValidateArticleRequest(ArticleImageRequest article, int index)
    {
        var errors = new List<string>();

        // Validate ArticleUrl
        if (string.IsNullOrWhiteSpace(article.ArticleUrl))
        {
            errors.Add($"Article[{index}]: ArticleUrl is required");
        }
        else
        {
            // Validate URL format and security
            if (!Uri.TryCreate(article.ArticleUrl, UriKind.Absolute, out var uri))
            {
                errors.Add($"Article[{index}]: ArticleUrl is not a valid URL");
            }
            else
            {
                // Security validation: only allow HTTP/HTTPS
                if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                {
                    errors.Add($"Article[{index}]: Only HTTP and HTTPS URLs are allowed");
                }

                // Block potentially dangerous domains
                var host = uri.Host.ToLowerInvariant();
                if (host == "localhost" || host == "127.0.0.1" || host.StartsWith("192.168.") || host.StartsWith("10."))
                {
                    errors.Add($"Article[{index}]: Internal/local network URLs are not allowed");
                }

                // URL length validation
                if (article.ArticleUrl.Length > 2000)
                {
                    errors.Add($"Article[{index}]: ArticleUrl cannot exceed 2000 characters");
                }
            }
        }

        // Validate optional fields
        if (article.ArticleTitle != null && article.ArticleTitle.Length > 500)
        {
            errors.Add($"Article[{index}]: ArticleTitle cannot exceed 500 characters");
        }

        if (article.ArticleDescription != null && article.ArticleDescription.Length > 1000)
        {
            errors.Add($"Article[{index}]: ArticleDescription cannot exceed 1000 characters");
        }

        if (article.UserAgent != null && article.UserAgent.Length > 200)
        {
            errors.Add($"Article[{index}]: UserAgent cannot exceed 200 characters");
        }

        // Validate timeout values
        if (article.TimeoutMs < 5000 || article.TimeoutMs > 120000)
        {
            errors.Add($"Article[{index}]: TimeoutMs must be between 5,000 and 120,000 milliseconds");
        }

        if (article.MaxImages <= 0 || article.MaxImages > 10)
        {
            errors.Add($"Article[{index}]: MaxImages must be between 1 and 10");
        }

        return errors;
    }
}
