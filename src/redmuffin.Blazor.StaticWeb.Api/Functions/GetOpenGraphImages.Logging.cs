using System.Net;
using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Api.Functions;

/// <summary>
///     LoggerMessage delegates for GetOpenGraphImages Azure Function.
/// </summary>
public partial class GetOpenGraphImages
{
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
}