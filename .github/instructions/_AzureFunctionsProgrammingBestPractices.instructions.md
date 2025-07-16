# Azure Functions Programming Best Practices for GitHub Copilot

- **Dependency Injection**: Use `Startup.cs` to register services (`ILogger`, `IHttpClientFactory`) in C# Azure Functions for testability and maintainability. Example: `builder.Services.AddSingleton<IMyService, MyService>();`.
- **Cold Start Optimization**: Minimize assembly size by reducing dependencies in C# projects. Use .NET Isolated Worker for better control over startup logic and avoid heavy initialization in function code.
- **Error Handling**: Implement retry policies with Polly for transient failures. Use try-catch blocks to handle exceptions gracefully and return meaningful HTTP status codes (e.g., `400` for bad requests) for HTTP triggers.
- **Input Validation**: Validate HTTP trigger inputs using C# model validation (e.g., `System.ComponentModel.DataAnnotations`) or custom checks to ensure security and prevent errors.
- **Structured Logging**: Use `ILogger` for structured logging in C#, capturing only essential data (e.g., request IDs, errors) to avoid performance overhead. Example: `logger.LogInformation("Processing {RequestId}", requestId);`.
- **Asynchronous Programming**: Use `async`/`await` in C# functions for I/O-bound operations (e.g., HTTP calls, database queries) to improve scalability. Avoid blocking calls like `.Result` or `.Wait()`.
- **Function Granularity**: Write single-responsibility functions. Split complex logic into smaller, focused functions to improve maintainability and reusability. Example: Separate data retrieval and processing into distinct functions.
- **Configuration Management**: Access settings via environment variables using `Environment.GetEnvironmentVariable` in C#. Avoid hardcoding values to ensure flexibility across environments.
- **Unit Testing**: Write unit tests for function logic using frameworks like xUnit or MSTest. Mock dependencies (e.g., `ILogger`, `IHttpClientFactory`) with Moq to isolate function behavior.
- **Idempotency**: Ensure functions are idempotent, especially for event-driven triggers (e.g., Queue, Event Hub). Handle duplicate messages gracefully using unique identifiers or state checks.
- **Parameter Optimization**: Use strongly-typed bindings (e.g., `QueueTrigger`, `BlobInput`) in C# to reduce parsing logic and improve type safety. Avoid overusing dynamic `JObject` inputs.
- **Resource Cleanup**: Dispose of resources (e.g., database connections, HTTP clients) properly using `IDisposable` or `using` statements to prevent memory leaks in long-running functions.
- **Code Reusability**: Extract shared logic into class libraries or static methods in C#. Use NuGet packages for cross-function utilities to maintain DRY principles.
- **Performance Monitoring**: Instrument code with custom metrics via Application Insights SDK in C# (e.g., `TelemetryClient.TrackMetric`) to track function-specific performance indicators.
- **Versioning**: For HTTP-triggered functions, implement API versioning (e.g., via query parameters or headers) to support backward compatibility as function logic evolves.
- **Secure Coding**: Sanitize inputs and outputs to prevent injection attacks (e.g., SQL, XSS). Use libraries like `AntiXssEncoder` for output encoding in HTTP responses.