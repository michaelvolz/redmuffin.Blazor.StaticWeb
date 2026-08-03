using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace redmuffin.Blazor.StaticWeb.Modules.AzureHealthCheck.Tests;

[Category("Feature:ApiHealth")]
public sealed partial class HealthCheckServiceTests
{
    private static TestScope CreateScope(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
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

        internal HealthCheckService Service => _serviceProvider.GetRequiredService<HealthCheckService>();

        public void Dispose() => _serviceProvider.Dispose();
    }

    private sealed class HttpClientFactory_Fake : IHttpClientFactory, IDisposable
    {
        private readonly ControlledHttpHandler_Fake _handler;

        public HttpClientFactory_Fake(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = new ControlledHttpHandler_Fake(handler);
        }

        public HttpClient CreateClient(string name)
        {
            var client = new HttpClient(_handler, disposeHandler: false);
            client.BaseAddress = new Uri("http://localhost/");
            return client;
        }

        public void Dispose() => _handler.Dispose();
    }

    public sealed class ControlledHttpHandler_Fake : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public ControlledHttpHandler_Fake(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request);
        }
    }
}
