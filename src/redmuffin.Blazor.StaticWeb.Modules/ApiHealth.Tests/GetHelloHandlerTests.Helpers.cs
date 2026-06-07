using Microsoft.Extensions.DependencyInjection;
using redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Contracts;

namespace redmuffin.Blazor.StaticWeb.Modules.ApiHealth.Tests;

[Category("Feature:ApiHealth")]
public sealed partial class GetHelloHandlerTests
{
    private static TestScope CreateScope(string response = "Hello!")
    {
        return new TestScope(response);
    }

    public sealed class TestScope : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IHealthCheckService_Fake _healthCheckService;

        public TestScope(string response)
        {
            _healthCheckService = new IHealthCheckService_Fake(response);

            var services = new ServiceCollection();
            services.AddSingleton<IHealthCheckService>(_healthCheckService);
            services.AddSingleton<GetHelloHandler>();
            _serviceProvider = services.BuildServiceProvider();
        }

        public IHealthCheckService_Fake HealthCheckService => _healthCheckService;

        public GetHelloHandler Handler => _serviceProvider.GetRequiredService<GetHelloHandler>();

        public void Dispose() => _serviceProvider.Dispose();
    }

    public sealed class IHealthCheckService_Fake : IHealthCheckService
    {
        private readonly string _response;

        public IHealthCheckService_Fake(string response)
        {
            _response = response;
        }

        public Exception? Exception { get; set; }

        public Task<string> GetHelloAsync(CancellationToken cancellationToken = default)
        {
            if (Exception is not null)
            {
                return Task.FromException<string>(Exception);
            }

            return Task.FromResult(_response);
        }
    }
}
