using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using redmuffin.Blazor.StaticWeb.Tests.Support.Http;
using redmuffin.Blazor.StaticWeb.Tests.Support.Logging;

namespace redmuffin.Blazor.StaticWeb.Modules.AzureHealthCheck.Tests;

[Category("Feature:ApiHealth")]
public sealed partial class HealthCheckServiceTests
{
    private static TestScope CreateScope(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> handler
    )
    {
        return new TestScope(handler);
    }

    public sealed class TestScope : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly Logger_Spy<HealthCheckService> _logger;
        private readonly HttpClientFactory_Fake _httpClientFactory;

        public TestScope(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _logger = new Logger_Spy<HealthCheckService>();
            _httpClientFactory = new HttpClientFactory_Fake(handler);

            var services = new ServiceCollection();
            services.AddSingleton<ILogger<HealthCheckService>>(_logger);
            services.AddSingleton<IHttpClientFactory>(_httpClientFactory);
            services.AddSingleton<HealthCheckService>();
            _serviceProvider = services.BuildServiceProvider();
        }

        public IReadOnlyList<LogEntry> LogEntries => _logger.LogEntries;

        internal HealthCheckService Service =>
            _serviceProvider.GetRequiredService<HealthCheckService>();

        public void Dispose() => _serviceProvider.Dispose();
    }
}
